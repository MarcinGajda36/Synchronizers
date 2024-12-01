namespace PerKeySynchronizers.UnboundedParallelism;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Unbounded parallelism.
/// Uses optimistic-locking to grab semaphore for key.
/// Allows to enter synchronized section for key B inside synchronized section of key A.
/// </summary>
public sealed class ConcurrentDictionaryOfSemaphores<TKey>(IEqualityComparer<TKey>? equalityComparer = null)
    : PerKeySynchronizer<TKey>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CountSemaphorePair> semaphores = new(equalityComparer);
    private readonly record struct CountSemaphorePair(int Count, SemaphoreSlim Semaphore);

    protected override SemaphoreSlim GetOrCreate(TKey key)
    {
        var semaphores_ = semaphores;
        while (true)
        {
            if (semaphores_.TryGetValue(key, out var old))
            {
                var incremented = old with { Count = old.Count + 1 };
                if (semaphores_.TryUpdate(key, incremented, old))
                {
                    return old.Semaphore;
                }
            }
            else
            {
                var @new = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
                if (semaphores_.TryAdd(key, @new))
                {
                    return @new.Semaphore;
                }
                else
                {
                    @new.Semaphore.Dispose();
                }
            }
        }
    }

    protected override void Cleanup(TKey key)
    {
        var semaphores_ = semaphores;
        while (true)
        {
            var old = semaphores_[key];
            if (old.Count == 1)
            {
                var toRemove = KeyValuePair.Create(key, old);
                if (semaphores_.TryRemove(toRemove))
                {
                    old.Semaphore.Dispose();
                    return;
                }
            }
            else
            {
                var decremented = old with { Count = old.Count - 1 };
                if (semaphores_.TryUpdate(key, decremented, old))
                {
                    return;
                }
            }
        }
    }
}