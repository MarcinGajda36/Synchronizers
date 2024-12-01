namespace PerKeySynchronizers.UnboundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;

public abstract class PerKeySynchronizer<TKey>
    : IPerKeySynchronizer<TKey>
    where TKey : notnull
{
    protected abstract SemaphoreSlim GetOrCreate(TKey key);
    protected abstract void Cleanup(TKey key);

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var semaphore = GetOrCreate(key);
        try
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
        finally
        {
            Cleanup(key);
        }
    }

    public Task SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            resultFactory,
            static (func, token) => func(token),
            cancellationToken);

    public Task SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            func,
            static async (func, token) =>
            {
                await func(token);
                return true;
            },
            cancellationToken);
}