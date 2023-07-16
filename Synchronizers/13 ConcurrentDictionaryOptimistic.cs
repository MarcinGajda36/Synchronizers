using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class ConcurrentDictionaryOptimistic<TKey>
    where TKey : notnull
{
    const nint TombStone = -1;

    private readonly record struct CountSemaphorePair(nint Count, SemaphoreSlim Semaphore);
    private readonly ConcurrentDictionary<TKey, CountSemaphorePair> semaphores;

    public ConcurrentDictionaryOptimistic(IEqualityComparer<TKey>? equalityComparer = null)
        => semaphores = new(equalityComparer);

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        while (true)
        {
            if (semaphores.TryGetValue(key, out var previous))
            {
                if (previous.Count == TombStone)
                    continue;

                var updated = previous with { Count = previous.Count + 1 };
                if (semaphores.TryUpdate(key, updated, previous))
                    return previous.Semaphore;
            }
            else
            {
                var newSemaphore = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
                if (semaphores.TryAdd(key, newSemaphore))
                    return newSemaphore.Semaphore;
                else
                    newSemaphore.Semaphore.Dispose();
            }
        }
    }

    private void Cleanup(TKey key)
    {
        while (true)
        {
            var previous = semaphores[key];
            if (previous.Count is 1)
            {
                var tombStoned = previous with { Count = TombStone };
                if (semaphores.TryUpdate(key, tombStoned, previous))
                {
                    semaphores.TryRemove(key, out _);
                    tombStoned.Semaphore.Dispose();
                    return;
                }
            }
            else
            {
                var @new = previous with { Count = previous.Count - 1 };
                if (semaphores.TryUpdate(key, @new, previous))
                    return;
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
}