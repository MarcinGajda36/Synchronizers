namespace PerKeySynchronizers.BoundedParallelism;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial struct PerKeySynchronizer
{
    private static void ReleaseAll(SemaphoreSlim[] pool, int index)
    {
        for (var toRelease = index; toRelease >= 0; --toRelease)
        {
            _ = pool[toRelease].Release();
        }
    }

    public Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
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
        ValidateDispose(pool_);
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

    public TResult SynchronizeAll<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        for (var index = 0; index < pool_.Length; ++index)
        {
            try
            {
                pool_[index].Wait(cancellationToken);
            }
            catch
            {
                ReleaseAll(pool_, index - 1);
                throw;
            }
        }

        try
        {
            return resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseAll(pool_, pool_.Length - 1);
        }
    }

    public void SynchronizeAll<TKey, TArgument>(
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAll(
            (argument, action),
            static (arguments, cancellationToken) =>
            {
                arguments.action(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public TResult SynchronizeAll<TKey, TResult>(
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAll(
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public void SynchronizeAll<TKey>(
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAll(
            action,
            static (func, cancellationToken) =>
            {
                func(cancellationToken);
                return true;
            },
            cancellationToken);
}
