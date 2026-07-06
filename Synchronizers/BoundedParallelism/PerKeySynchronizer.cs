namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Runtime.CompilerServices;
using System.Threading;

public partial struct PerKeySynchronizer
    : IPerKeySynchronizer, IDisposable, IEquatable<PerKeySynchronizer>
{
    private const int DefaultMaxDegreeOfParallelism = 67;

    private SemaphoreSlim[] pool;

    /// <summary>
    /// Synchronizes operations so all operation on given key happen one at a time, 
    /// while allowing operations for different keys to happen in parallel.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">
    /// Maximum total parallel operation. Has to be at least 1. Prime number is recommended but not necessary.
    /// </param>
    public PerKeySynchronizer(int maxDegreeOfParallelism = DefaultMaxDegreeOfParallelism)
    {
        ValidateSize(maxDegreeOfParallelism);
        pool = CreatePool(maxDegreeOfParallelism);
    }

    /// <summary>
    /// Synchronizes operations so all operation on given key happen one at a time, 
    /// while allowing operations for different keys to happen in parallel.
    /// Defaults to same maxDegreeOfParallelism as argument taking constructor.
    /// </summary>
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
        if (maxDegreeOfParallelism < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxDegreeOfParallelism),
                maxDegreeOfParallelism,
                "Max degree of parallelism has to be at least 1.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetKeyIndex<TKey>(TKey key, int poolLength)
        where TKey : notnull
        => key.GetHashCode() % poolLength;

    public void Dispose()
    {
        var original = Interlocked.Exchange(ref pool!, null);
        if (original != null)
        {
            Array.ForEach(original, static semaphore => semaphore.Dispose());
        }
    }

    private static void ValidateDispose(SemaphoreSlim[]? pool)
        => ObjectDisposedException.ThrowIf(pool == null, typeof(PerKeySynchronizer));

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
