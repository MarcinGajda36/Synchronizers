namespace Synchronizers;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Unbounded parallelism.
/// Uses lock to grab semaphore for key.
/// Allows to enter synchronized section for key B inside synchronized section of key A.
/// </summary>
public class DictionaryOfSemaphores<TKey>(IEqualityComparer<TKey>? equalityComparer = null)
    where TKey : notnull
{
    private readonly Dictionary<TKey, CountSemaphorePair> semaphores = new(equalityComparer);

    private struct CountSemaphorePair(int count, SemaphoreSlim semaphore)
    {
        public int Count = count;
        public readonly SemaphoreSlim Semaphore = semaphore;
    }

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        lock (semaphores)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(semaphores, key, out var exists);
            if (exists)
            {
                ++pair.Count;
            }
            else
            {
                pair = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
            }

            return pair.Semaphore;
        }
    }

    private void Cleanup(TKey key)
    {
        lock (semaphores)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrNullRef(semaphores, key);
            ref var count = ref pair.Count;
            if (count == 1)
            {
                pair.Semaphore.Dispose();
                _ = semaphores.Remove(key);
            }
            else
            {
                --count;
            }
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> func,
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
            _ = semaphore.Release();
            Cleanup(key);
        }
    }

    public Task SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);
}