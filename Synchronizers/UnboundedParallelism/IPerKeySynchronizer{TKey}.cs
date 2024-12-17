namespace PerKeySynchronizers.UnboundedParallelism;
using System;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer<TKey>
    where TKey : notnull
{
    ValueTask<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    ValueTask SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    ValueTask<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    ValueTask SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    TResult Synchronize<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    void Synchronize<TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default);

    TResult Synchronize<TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    void Synchronize(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default);
}
