namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Numerics;
using System.Threading;

public sealed partial class PerKeySynchronizer
    : IPerKeySynchronizer, IDisposable
{
    private SemaphoreSlim[] pool;

    /// <summary>
    /// Synchronizes operations so all operation on given key happen one at a time, 
    /// while allowing operations for different keys to happen in parallel.
    /// Uses Fibonacci hashing to grab semaphore for given key.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum total parallel operation. 
    /// Has to be at least 1 and a power of 2.
    /// </param>
    public PerKeySynchronizer(int maxDegreeOfParallelism = 32)
    {
        var error = ValidateSize(maxDegreeOfParallelism);
        if (error != null)
        {
            throw error;
        }

        pool = CreatePool(maxDegreeOfParallelism);
    }

    private static SemaphoreSlim[] CreatePool(int maxDegreeOfParallelism)
    {
        var pool = new SemaphoreSlim[maxDegreeOfParallelism];
        for (var index = 0; index < pool.Length; ++index)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        return pool;
    }

    private static ArgumentOutOfRangeException? ValidateSize(int maxDegreeOfParallelism)
        => maxDegreeOfParallelism < 1 || BitOperations.IsPow2(maxDegreeOfParallelism) is false
        ? new ArgumentOutOfRangeException(
            nameof(maxDegreeOfParallelism),
            maxDegreeOfParallelism,
            "Max degree of parallelism has to be at least 1 and a power of 2.")
        : null;

    private static int GetKeyIndex<TKey>(TKey key, int poolLength)
        where TKey : notnull
        => key.GetHashCode() & (poolLength - 1);

    public void Dispose()
    {
        var original = Interlocked.Exchange(ref pool!, null);
        if (original != null)
        {
            Array.ForEach(original, semaphore => semaphore.Dispose());
        }
    }
}
