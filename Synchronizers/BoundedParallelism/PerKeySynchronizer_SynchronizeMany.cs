namespace PerKeySynchronizers.BoundedParallelism;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public partial struct PerKeySynchronizer
{
    private static int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int poolLength, int[] keysIndexes)
        where TKey : notnull
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            var index = GetKeyIndex(key, poolLength);
            if (keysIndexes.AsSpan(..keyCount).Contains(index) is false)
            {
                keysIndexes[keyCount++] = index;
                if (keyCount == poolLength)
                {
                    break;
                }
            }
        }

        Array.Sort(keysIndexes, 0, keyCount);
        return keyCount;
    }

    private static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
    {
        for (var index = locked.Length - 1; index >= 0; --index)
        {
            // I didn't saw strict need to release in reverse order, it just seemed beneficial
            _ = pool[locked[index]].Release();
        }
    }

    public readonly Task<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async Task<TResult> Core(
            SemaphoreSlim[] pool,
            IEnumerable<TKey> keys,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var poolLength = pool.Length;
            var indexes = ArrayPool<int>.Shared.Rent(poolLength);
            var indexesCount = FillWithKeyIndexes(keys, poolLength, indexes);
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
        ValidateDispose(pool_);
        return Core(pool_, keys, argument, resultFactory, cancellationToken);
    }

    public readonly Task SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async Task Core(
            SemaphoreSlim[] pool,
            IEnumerable<TKey> keys,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask> func,
            CancellationToken cancellationToken)
        {
            var poolLength = pool.Length;
            var indexes = ArrayPool<int>.Shared.Rent(poolLength);
            var indexesCount = FillWithKeyIndexes(keys, poolLength, indexes);
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
                await func(argument, cancellationToken);
            }
            finally
            {
                ReleaseLocked(pool, indexes.AsSpan(..indexesCount));
                ArrayPool<int>.Shared.Return(indexes);
            }
        }

        var pool_ = pool;
        ValidateDispose(pool_);
        return Core(pool_, keys, argument, func, cancellationToken);
    }

    public readonly Task<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public readonly Task SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            func,
            static (func, cancellationToken) => func(cancellationToken),
            cancellationToken);

    public readonly TResult SynchronizeMany<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var poolLength = pool_.Length;
        var indexes = ArrayPool<int>.Shared.Rent(poolLength);
        var indexesCount = FillWithKeyIndexes(keys, poolLength, indexes);
        for (var index = 0; index < indexesCount; ++index)
        {
            try
            {
                pool_[indexes[index]].Wait(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool_, indexes.AsSpan(..index));
                ArrayPool<int>.Shared.Return(indexes);
                throw;
            }
        }

        try
        {
            return resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool_, indexes.AsSpan(..indexesCount));
            ArrayPool<int>.Shared.Return(indexes);
        }
    }

    public readonly void SynchronizeMany<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var poolLength = pool_.Length;
        var indexes = ArrayPool<int>.Shared.Rent(poolLength);
        var indexesCount = FillWithKeyIndexes(keys, poolLength, indexes);
        for (var index = 0; index < indexesCount; ++index)
        {
            try
            {
                pool_[indexes[index]].Wait(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool_, indexes.AsSpan(..index));
                ArrayPool<int>.Shared.Return(indexes);
                throw;
            }
        }

        try
        {
            action(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool_, indexes.AsSpan(..indexesCount));
            ArrayPool<int>.Shared.Return(indexes);
        }
    }

    public readonly TResult SynchronizeMany<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeMany(
            keys,
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public readonly void SynchronizeMany<TKey>(
        IEnumerable<TKey> keys,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeMany(
            keys,
            action,
            static (func, cancellationToken) => func(cancellationToken),
            cancellationToken);

}
