namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer
{
    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="func"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="resultFactory">Async factory invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="func">Async action invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="action"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="resultFactory">Factory invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronization for a single <paramref name="key"/>, execute <paramref name="action"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="key">The key for acquiring synchronization.</param>
    /// <param name="action">Action invoked while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="func"/> once with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="resultFactory">Async factory invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="func"/> once.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="func">Async action invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult SynchronizeMany<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="action"/> once with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>>
    void SynchronizeMany<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="resultFactory">Factory invoked while holding the all keys synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult SynchronizeMany<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations for <paramref name="keys"/>, execute <paramref name="action"/> once.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    void SynchronizeMany<TKey>(
        IEnumerable<TKey> keys,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="func"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeAllAsync<TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="resultFactory">Async factory invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask<TResult> SynchronizeAllAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="func"/>,
    /// and return its result.
    /// </summary>
    /// <param name="func">Async factory invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes with the <paramref name="func"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    ValueTask SynchronizeAllAsync(
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult SynchronizeAll<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="action"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    void SynchronizeAll<TArgument>(
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="resultFactory">Factory invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    TResult SynchronizeAll<TResult>(
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizations, execute <paramref name="action"/>.
    /// </summary>
    /// <param name="action">Action invoked while holding the all synchronizations.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller holding the same synchronization will run concurrently.
    /// </remarks>
    void SynchronizeAll(
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupResult"/> once per group while holding the group's semaphore.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's delegate.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize. Keys that map to the same internal semaphore
    /// are processed together by a single invocation of <paramref name="perGroupResult"/>.</param>
    /// <param name="argument">An argument passed through to each group's delegate.</param>
    /// <param name="perGroupResult">Delegate invoked for each group. Receives the supplied <paramref name="argument"/>,
    /// the keys belonging to that group, and a <see cref="CancellationToken"/>.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>
    /// A <see cref="Task{TResult[]}"/> that completes when all group delegates complete. The returned array contains
    /// the result produced by <paramref name="perGroupResult"/> for each group. The ordering corresponds to the order
    /// in which groups were enumerated internally.
    /// </returns>
    /// <remarks>
    /// - Keys are assigned to groups based on the internal semaphore index computed by <c>GetKeyIndex</c>.
    /// - Each group's delegate is invoked while holding that group's <see cref="SemaphoreSlim"/>, ensuring that
    ///   no two group delegates that map to the same semaphore run concurrently.
    /// - Implementations validate the synchronizer is not disposed before proceeding.
    /// - If <paramref name="cancellationToken"/> is signaled, waiting for semaphores or delegates may throw
    ///   <see cref="OperationCanceledException"/>.
    /// </remarks>
    Task<TResult[]> SynchronizeGroupedAsync<TKey, TArgument, TResult>(IEnumerable<TKey> keys, TArgument argument, Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupFunc"/> (an async action without a result) once per group while holding the group's semaphore.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's delegate.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="argument">An argument passed through to each group's delegate.</param>
    /// <param name="perGroupFunc">Async action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>A <see cref="Task"/> that completes when all group actions complete.</returns>
    Task SynchronizeGroupedAsync<TKey, TArgument>(IEnumerable<TKey> keys, TArgument argument, Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupResult"/> once per group while holding the group's semaphore.
    /// This overload does not pass an extra argument to the per-group delegate.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupResult">Async function invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>
    /// A <see cref="Task{TResult[]}"/> that completes when all group delegates complete. The returned array contains
    /// the result produced by <paramref name="perGroupResult"/> for each group.
    /// </returns>
    Task<TResult[]> SynchronizeGroupedAsync<TKey, TResult>(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupFunc"/> (an async action without a result) once per group while holding the group's semaphore.
    /// This overload does not pass an extra argument to the per-group delegate.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupFunc">Async action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>A <see cref="Task"/> that completes when all group actions complete.</returns>
    Task SynchronizeGroupedAsync<TKey>(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Synchronously groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupResult"/> once per group while holding the group's semaphore.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's delegate.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize. Implementations may use PLINQ or other mechanisms to execute groups in parallel.</param>
    /// <param name="argument">An argument passed through to each group's delegate.</param>
    /// <param name="perGroupResult">Delegate invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>An array with the per-group results.</returns>
    TResult[] SynchronizeGrouped<TKey, TArgument, TResult>(IEnumerable<TKey> keys, TArgument argument, Func<TArgument, IEnumerable<TKey>, CancellationToken, TResult> perGroupResult, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Synchronously groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupAction"/> once per group while holding the group's semaphore.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's action.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="argument">An argument passed through to each group's action.</param>
    /// <param name="perGroupAction">Action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    void SynchronizeGrouped<TKey, TArgument>(IEnumerable<TKey> keys, TArgument argument, Action<TArgument, IEnumerable<TKey>, CancellationToken> perGroupAction, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Synchronously groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupResult"/> once per group while holding the group's semaphore. This overload does not pass an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupResult">Function invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    /// <returns>An array with the per-group results.</returns>
    TResult[] SynchronizeGrouped<TKey, TResult>(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, CancellationToken, TResult> perGroupResult, CancellationToken cancellationToken = default) where TKey : notnull;

    /// <summary>
    /// Synchronously groups the provided <paramref name="keys"/> by the internal semaphore index and invokes the supplied
    /// <paramref name="perGroupAction"/> once per group while holding the group's semaphore. This overload does not pass an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupAction">Action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token used to cancel waiting for semaphores and group processing.</param>
    void SynchronizeGrouped<TKey>(IEnumerable<TKey> keys, Action<IEnumerable<TKey>, CancellationToken> perGroupAction, CancellationToken cancellationToken = default) where TKey : notnull;
}