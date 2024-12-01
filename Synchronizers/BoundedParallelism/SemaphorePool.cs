namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;

internal static class SemaphorePool
{
    public static SemaphoreSlim[] CreatePool(int size)
    {
        var pool = new SemaphoreSlim[size];
        for (var index = 0; index < pool.Length; ++index)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        return pool;
    }

    public static async Task<TResult> SynchronizeAsync<TArgument, TResult>(
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

    public static async Task<TResult> SynchronizeManyAsync<TArgument, TResult>(
        SemaphoreSlim[] pool,
        int[] indexes,
        int indexesCount,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken)
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (var index = locked.Length - 1; index >= 0; --index)
            {
                // I didn't saw strict need to release in reverse order, it just seemed beneficial
                _ = pool[locked[index]].Release();
            }
        }

        for (var index = 0; index < indexesCount; ++index)
        {
            try
            {
                await pool[indexes[index]].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool, indexes.AsSpan(..index));
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
        }
    }

    public static async Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        SemaphoreSlim[] pool,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken)
    {
        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (var toRelease = index; toRelease >= 0; --toRelease)
            {
                _ = pool[toRelease].Release();
            }
        }
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
}
