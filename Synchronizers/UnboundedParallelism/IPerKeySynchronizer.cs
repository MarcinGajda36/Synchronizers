namespace PerKeySynchronizers.UnboundedParallelism;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer<TKey>
    where TKey : notnull
{
    Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    Task SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    Task SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);
}
