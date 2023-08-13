using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;

// Maximizes paralelizm?
// Dictionaries also make it safe to use SynchronizeAsync inside of other key SynchronizeAsync and pools could dead-lock on it.
public class ConcurrentDictionaryOptimistic<TKey>
    where TKey : notnull
{
    private readonly record struct CountSemaphorePair(nint Count, SemaphoreSlim Semaphore);
    private readonly ConcurrentDictionary<TKey, CountSemaphorePair> semaphores;

    public ConcurrentDictionaryOptimistic(IEqualityComparer<TKey>? equalityComparer = null)
        => semaphores = new(equalityComparer);

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        // If i put this loop inside SynchronizeAsync then maybe i can see some additional perf improvements.
        while (true)
        {
            if (semaphores.TryGetValue(key, out var old))
            {
                var incremented = old with { Count = old.Count + 1 };
                if (semaphores.TryUpdate(key, incremented, old))
                    return old.Semaphore;
            }
            else
            {
                var @new = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
                if (semaphores.TryAdd(key, @new))
                    return @new.Semaphore;
                else
                    @new.Semaphore.Dispose();
            }
        }
    }

    // There is a choice here between passing only key and staring from value lookup or passing pair and trying to update with it.
    private void Cleanup(TKey key)
    {
        while (true)
        {
            var old = semaphores[key];
            if (old.Count == 1)
            {
                var toRemove = KeyValuePair.Create(key, old);
                if (semaphores.TryRemove(toRemove))
                {
                    old.Semaphore.Dispose();
                    return;
                }
            }
            else
            {
                var decremented = old with { Count = old.Count - 1 };
                if (semaphores.TryUpdate(key, decremented, old))
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