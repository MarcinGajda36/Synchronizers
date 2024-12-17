namespace PerKeySynchronizers.BoundedParallelism;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial struct PerKeySynchronizer
{
    public readonly ValueTask<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async ValueTask<TResult> Core(
            SemaphoreSlim[] pool,
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var index = GetKeyIndex(key, pool.Length);
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
        ValidateDispose(pool_);
        return Core(pool_, key, argument, resultFactory, cancellationToken);
    }

    public readonly ValueTask SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async ValueTask Core(
            SemaphoreSlim[] pool,
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask> func,
            CancellationToken cancellationToken)
        {
            var index = GetKeyIndex(key, pool.Length);
            var semaphore = pool[index];
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await func(argument, cancellationToken);
            }
            finally
            {
                _ = semaphore.Release();
            }
        }

        var pool_ = pool;
        ValidateDispose(pool_);
        return Core(pool_, key, argument, func, cancellationToken);
    }

    public readonly ValueTask<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    public readonly ValueTask SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(key, func, static (func, token) => func(token), cancellationToken);

    public readonly TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var index = GetKeyIndex(key, pool_.Length);
        var semaphore = pool_[index];
        semaphore.Wait(cancellationToken);
        try
        {
            return resultFactory(argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public readonly void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var index = GetKeyIndex(key, pool_.Length);
        var semaphore = pool_[index];
        semaphore.Wait(cancellationToken);
        try
        {
            action(argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public readonly TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    public readonly void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(key, action, (action, token) => action(token), cancellationToken);
}
