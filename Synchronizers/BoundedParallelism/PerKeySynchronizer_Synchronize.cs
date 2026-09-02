namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;

public partial struct PerKeySynchronizer
{
    /// <inheritdoc/>
    public readonly ValueTask<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var semaphore = pool_[GetKeyIndex(key, pool_.Length)];
        return Core(semaphore, argument, resultFactory, cancellationToken);

        static async ValueTask<TResult> Core(
            SemaphoreSlim semaphore,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
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
    }

    /// <inheritdoc/>
    public readonly ValueTask SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var semaphore = pool_[GetKeyIndex(key, pool_.Length)];
        return Core(semaphore, argument, func, cancellationToken);

        static async ValueTask Core(
            SemaphoreSlim semaphore,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask> func,
            CancellationToken cancellationToken)
        {
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
    }

    /// <inheritdoc/>
    public readonly ValueTask<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    /// <inheritdoc/>
    public readonly ValueTask SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(key, func, static (func, token) => func(token), cancellationToken);

    /// <inheritdoc/>
    public readonly TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var semaphore = pool_[GetKeyIndex(key, pool_.Length)];
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

    /// <inheritdoc/>
    public readonly void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var semaphore = pool_[GetKeyIndex(key, pool_.Length)];
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

    /// <inheritdoc/>
    public readonly TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    /// <inheritdoc/>
    public readonly void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(key, action, static (action, token) => action(token), cancellationToken);
}
