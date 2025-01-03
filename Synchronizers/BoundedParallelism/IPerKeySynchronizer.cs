namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer
{
    ValueTask<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    TResult SynchronizeMany<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    void SynchronizeMany<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    TResult SynchronizeMany<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    void SynchronizeMany<TKey>(
        IEnumerable<TKey> keys,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    ValueTask<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    ValueTask SynchronizeAllAsync<TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    ValueTask<TResult> SynchronizeAllAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    ValueTask SynchronizeAllAsync(
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    TResult SynchronizeAll<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    void SynchronizeAll<TArgument>(
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default);

    TResult SynchronizeAll<TResult>(
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    void SynchronizeAll(
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default);
}
