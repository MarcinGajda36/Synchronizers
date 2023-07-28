using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class DictionaryOfSemaphores<TKey>
    where TKey : notnull
{
    private readonly record struct CountSemaphorePair(nuint Count, SemaphoreSlim Semaphore);
    private readonly Dictionary<TKey, CountSemaphorePair> semaphores;

    public DictionaryOfSemaphores(IEqualityComparer<TKey>? equalityComparer = null)
        => semaphores = new(equalityComparer);

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        lock (semaphores)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(semaphores, key, out var exists);
            pair = exists
                ? pair with { Count = pair.Count + 1 }
                : new CountSemaphorePair(1, new SemaphoreSlim(1, 1));

            return pair.Semaphore;
        }
    }

    private void Cleanup(TKey key)
    {
        lock (semaphores)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrNullRef(semaphores, key);
            if (pair.Count == 1)
            {
                pair.Semaphore.Dispose();
                semaphores.Remove(key);
            }
            else
            {
                pair = pair with { Count = pair.Count - 1 };
            }
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> func,
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetOrCreate(key);
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await func(argument, cancellationToken);
        }
        finally
        {
            semaphore.Release();
            Cleanup(key);
        }
    }

    public Task SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return false;
            },
            cancellationToken);
}