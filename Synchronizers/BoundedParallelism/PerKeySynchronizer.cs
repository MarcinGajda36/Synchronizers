namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public sealed class PerKeySynchronizer
    : IPerKeySynchronizer, IDisposable
{
    private SemaphoreSlim[] pool;
    private readonly int poolIndexBitShift;

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
        poolIndexBitShift = 32 - BitOperations.TrailingZeroCount(maxDegreeOfParallelism);
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

    private static int GetKeyIndex<TKey>(TKey key, int poolIndexBitShift)
        where TKey : notnull
    {
        const uint Fibonacci = 2654435769u; // 2 ^ 32 / PHI
        unchecked
        {
            if (poolIndexBitShift != 32)
            {
                var keyHash = (uint)key.GetHashCode();
                var fibonacciHash = keyHash * Fibonacci;
                var index = fibonacciHash >> poolIndexBitShift;
                return (int)index;
            }
            else
            {
                return 0;
            }
        }
    }

    public Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async Task<TResult> Core(
            int poolIndexBitShift,
            SemaphoreSlim[] pool,
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var index = GetKeyIndex(key, poolIndexBitShift);
            var semaphore = pool[index];
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await resultFactory(argument, cancellationToken);
            }
            finally
            {
                _ = semaphore.Release();
            }
        }

        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return Core(poolIndexBitShift, pool_, key, argument, resultFactory, cancellationToken);
    }

    public Task SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            resultFactory,
            static (func, token) => func(token),
            cancellationToken);

    public Task SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            func,
            static async (func, token) =>
            {
                await func(token);
                return true;
            },
            cancellationToken);

    private static int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int poolIndexBitShift, int[] keysIndexes)
        where TKey : notnull
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            var index = GetKeyIndex(key, poolIndexBitShift);
            if (keysIndexes.AsSpan(..keyCount).Contains(index) is false)
            {
                keysIndexes[keyCount++] = index;
            }
        }

        Array.Sort(keysIndexes, 0, keyCount);
        return keyCount;
    }

    public Task<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (var index = locked.Length - 1; index >= 0; --index)
            {
                // I didn't saw strict need to release in reverse order, it just seemed beneficial
                _ = pool[locked[index]].Release();
            }
        }

        static async Task<TResult> Core(
            int poolIndexBitShift,
            SemaphoreSlim[] pool,
            IEnumerable<TKey> keys,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var indexes = ArrayPool<int>.Shared.Rent(pool.Length);
            var indexesCount = FillWithKeyIndexes(keys, poolIndexBitShift, indexes);
            for (var index = 0; index < indexesCount; ++index)
            {
                try
                {
                    await pool[indexes[index]].WaitAsync(cancellationToken);
                }
                catch
                {
                    ReleaseLocked(pool, indexes.AsSpan(..index));
                    ArrayPool<int>.Shared.Return(indexes);
                    throw;
                }
            }

            try
            {
                return await resultFactory(argument, cancellationToken);
            }
            finally
            {
                ReleaseLocked(pool, indexes.AsSpan(..indexesCount));
                ArrayPool<int>.Shared.Return(indexes);
            }
        }

        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return Core(poolIndexBitShift, pool_, keys, argument, resultFactory, cancellationToken);
    }

    public Task SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public Task SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            func,
            static async (func, cancellationToken) =>
            {
                await func(cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (var toRelease = index; toRelease >= 0; --toRelease)
            {
                _ = pool[toRelease].Release();
            }
        }

        static async Task<TResult> Core(
            SemaphoreSlim[] pool,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            for (var index = 0; index < pool.Length; ++index)
            {
                try
                {
                    await pool[index].WaitAsync(cancellationToken);
                }
                catch
                {
                    ReleaseAll(pool, index - 1);
                    throw;
                }
            }

            try
            {
                return await resultFactory(argument, cancellationToken);
            }
            finally
            {
                ReleaseAll(pool, pool.Length - 1);
            }
        }

        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return Core(pool_, argument, resultFactory, cancellationToken);
    }

    public Task SynchronizeAllAsync<TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAllAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public Task SynchronizeAllAsync(
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            func,
            static async (func, cancellationToken) =>
            {
                await func(cancellationToken);
                return true;
            },
            cancellationToken);

    public void Dispose()
    {
        var original = Interlocked.Exchange(ref pool!, null);
        if (original != null)
        {
            Array.ForEach(original, semaphore => semaphore.Dispose());
        }
    }
}
