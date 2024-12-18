namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

public partial struct PerKeySynchronizer
    : IPerKeySynchronizer, IDisposable, IEquatable<PerKeySynchronizer>
{
    private const int DefaultMaxDegreeOfParallelism = 32;

    private SemaphoreSlim[] pool;

    /// <summary>
    /// Synchronizes operations so all operation on given key happen one at a time, 
    /// while allowing operations for different keys to happen in parallel.
    /// Uses Bit Mask to grab semaphore for given key.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum total parallel operation. 
    /// Has to be at least 1 and a power of 2.
    /// </param>
    public PerKeySynchronizer(int maxDegreeOfParallelism = DefaultMaxDegreeOfParallelism)
    {
        ValidateSize(maxDegreeOfParallelism);
        pool = CreatePool(maxDegreeOfParallelism);
    }

    public PerKeySynchronizer() : this(DefaultMaxDegreeOfParallelism) { }

    private static SemaphoreSlim[] CreatePool(int maxDegreeOfParallelism)
    {
        var pool = new SemaphoreSlim[maxDegreeOfParallelism];
        for (var index = 0; index < pool.Length; ++index)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        return pool;
    }

    private static void ValidateSize(int maxDegreeOfParallelism)
    {
        if (maxDegreeOfParallelism < 1 || BitOperations.IsPow2(maxDegreeOfParallelism) is false)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                maxDegreeOfParallelism,
                "Max degree of parallelism has to be at least 1 and a power of 2.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    private static void ValidateDispose(SemaphoreSlim[]? pool)
    {
        if (pool == null)
        {
            throw new ObjectDisposedException(typeof(PerKeySynchronizer).FullName);
        }
    }

    public readonly bool Equals(PerKeySynchronizer other)
        => ReferenceEquals(pool, other.pool);
    public override readonly bool Equals(object? obj)
        => obj is PerKeySynchronizer other && Equals(other);
    public static bool operator ==(PerKeySynchronizer left, PerKeySynchronizer right)
        => left.Equals(right);
    public static bool operator !=(PerKeySynchronizer left, PerKeySynchronizer right)
        => !left.Equals(right);
    public override readonly int GetHashCode()
        => pool.GetHashCode();
}
