namespace PerKeySynchronizers.UnboundedParallelism;

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

/// <summary>
/// Unbounded parallelism.
/// Uses lock to grab semaphore for key.
/// Allows to enter synchronized section for key B inside synchronized section of key A.
/// </summary>
public sealed class DictionaryOfSemaphores<TKey>(IEqualityComparer<TKey>? equalityComparer = null)
    : PerKeySynchronizer<TKey>
    where TKey : notnull
{
    private readonly Dictionary<TKey, CountSemaphorePair> semaphores = new(equalityComparer);

    private struct CountSemaphorePair(int count, SemaphoreSlim semaphore)
    {
        public int Count = count;
        public readonly SemaphoreSlim Semaphore = semaphore;
    }

    protected override SemaphoreSlim GetOrCreate(TKey key)
    {
        var semaphores_ = semaphores;
        lock (semaphores_)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrAddDefault(semaphores_, key, out var exists);
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

    protected override void Cleanup(TKey key)
    {
        var semaphores_ = semaphores;
        lock (semaphores_)
        {
            ref var pair = ref CollectionsMarshal.GetValueRefOrNullRef(semaphores_, key);
            ref var count = ref pair.Count;
            if (count == 1)
            {
                pair.Semaphore.Dispose();
                _ = semaphores_.Remove(key);
            }
            else
            {
                --count;
            }
        }
    }
}