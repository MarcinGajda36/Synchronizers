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

    private CountSemaphorePair GetOrCreate(TKey key)
    {
        while (true)
        {
            if (semaphores.TryGetValue(key, out var old))
            {
                if (old.Count == TombStone)
                    continue;

                var incremented = old with { Count = old.Count + 1 };
                if (semaphores.TryUpdate(key, incremented, old))
                    return old;
            }
            else
            {
                var @new = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
                if (semaphores.TryAdd(key, @new))
                    return @new;
                else
                    @new.Semaphore.Dispose();
            }
        }
    }

    private void Cleanup(TKey key, CountSemaphorePair old)
    {
        while (true)
        {
            if (old.Count == 1)
            {
                var tombStoned = old with { Count = TombStone };
                if (semaphores.TryUpdate(key, tombStoned, old))
                {
                    semaphores.TryRemove(key, out _);
                    tombStoned.Semaphore.Dispose();
                    return;
                }
            }
            else
            {
                var decremented = old with { Count = old.Count - 1 };
                if (semaphores.TryUpdate(key, decremented, old))
                    return;
            }
            old = semaphores[key];
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> func,
        CancellationToken cancellationToken = default)
    {
        var pair = GetOrCreate(key);
        await pair.Semaphore.WaitAsync(cancellationToken);
        try
        {
            return await func(argument, cancellationToken);
        }
        finally
        {
            pair.Semaphore.Release();
            Cleanup(key, pair);
        }
    }
}