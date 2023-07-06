using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class DictionaryOfSemaphores<TKey>
    where TKey : notnull
{
    private readonly record struct SemaphoreCountPair(nuint Count, SemaphoreSlim Semaphore);
    private readonly Dictionary<TKey, SemaphoreCountPair> semaphores;

    public DictionaryOfSemaphores(IEqualityComparer<TKey>? equalityComparer)
        => semaphores = new(equalityComparer);

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        lock (semaphores)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(semaphores, key, out var exists);
            pair = exists
                ? pair with { Count = pair.Count + 1 }
                : new SemaphoreCountPair(1, new SemaphoreSlim(1, 1));

            var semaphore = pair.Semaphore;
            return semaphore;
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

    public async Task ExecuteActionAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> func,
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetOrCreate(key);
        await semaphore.WaitAsync();
        try
        {
            await func(argument, cancellationToken);
        }
        finally
        {
            semaphore.Release();
            Cleanup(key);
        }
    }
}