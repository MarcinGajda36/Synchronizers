using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class DictionaryOfSemaphores<TKey>
    where TKey : notnull
{
    private readonly record struct SemaphoreCountPair(SemaphoreSlim Semaphore, int Count);
    private readonly Dictionary<TKey, SemaphoreCountPair> semaphores;

    public DictionaryOfSemaphores(IEqualityComparer<TKey>? equalityComparer)
        => semaphores = new(equalityComparer);

    private SemaphoreSlim GetOrCreate(TKey key)
    {
        lock (semaphores)
        {
            var pair = semaphores.TryGetValue(key, out var semaphore)
                ? semaphore with { Count = semaphore.Count + 1 }
                : new SemaphoreCountPair(new SemaphoreSlim(1, 1), 1);

            semaphores[key] = pair;
            return pair.Semaphore;
        }
    }

    private void Cleanup(TKey key)
    {
        lock (semaphores)
        {
            if (semaphores.TryGetValue(key, out var pair))
            {
                var newCount = pair.Count - 1;
                if (newCount == 0)
                {
                    semaphores.Remove(key);
                    pair.Semaphore.Dispose();
                }
                semaphores[key] = pair with { Count = newCount };
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