namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer
{
    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="func"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="resultFactory">Async factory invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="func"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="func">Async action invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="action"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="resultFactory">Factory invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire synchronizer for a single <paramref name="key"/>, execute <paramref name="action"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="key">The key for acquiring synchronizer.</param>
    /// <param name="action">Action invoked while holding the key's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="func"/> once with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="resultFactory">Async factory invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="func"/> once.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="func">Async action invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult SynchronizeMany<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="action"/> once with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>>
    void SynchronizeMany<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="resultFactory"/> once,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="resultFactory">Factory invoked while holding the all keys synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult SynchronizeMany<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers for <paramref name="keys"/>, execute <paramref name="action"/> once.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void SynchronizeMany<TKey>(
        IEnumerable<TKey> keys,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Async factory invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="func"/> with the supplied <paramref name="argument"/>.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="func"/>.</param>
    /// <param name="func">Async action invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeAllAsync<TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="resultFactory">Async factory invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask<TResult> SynchronizeAllAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="func"/>,
    /// and return its result.
    /// </summary>
    /// <param name="func">Async factory invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the <paramref name="func"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    ValueTask SynchronizeAllAsync(
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="resultFactory"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult SynchronizeAll<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="action"/> with the supplied <paramref name="argument"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="argument">An extra argument passed to the <paramref name="action"/>.</param>
    /// <param name="action">Action invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void SynchronizeAll<TArgument>(
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="resultFactory"/>,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="resultFactory">Factory invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>The <paramref name="resultFactory"/> result.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult SynchronizeAll<TResult>(
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire all synchronizers, execute <paramref name="action"/>.
    /// </summary>
    /// <param name="action">Action invoked while holding all synchronizers.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void SynchronizeAll(
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupResult"/> with the supplied <paramref name="argument"/> once per group. 
    /// Different groups are executed concurrently. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it, and returns its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="perGroupResult"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="perGroupResult"/>.</param>
    /// <param name="perGroupResult">Async factory invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="Task{TResult[]}"/> that completes with all the <paramref name="perGroupResult"/> results.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    Task<TResult[]> SynchronizeGroupedAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupFunc"/> with the supplied <paramref name="argument"/> once per group. 
    /// Different groups are executed concurrently. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="perGroupFunc"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="perGroupFunc"/>.</param>
    /// <param name="perGroupFunc">Async action invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="Task"/> that completes when all <paramref name="perGroupFunc"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    Task SynchronizeGroupedAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupResult"/> once per group. 
    /// Different groups are executed concurrently. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it, and returns its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="perGroupResult">Async factory invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="Task{TResult[]}"/> that completes with all the <paramref name="perGroupResult"/> results.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    Task<TResult[]> SynchronizeGroupedAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupFunc"/> once per group. 
    /// Different groups are executed concurrently. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="perGroupFunc">Async action invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>A <see cref="Task"/> that completes when all <paramref name="perGroupFunc"/> finishes.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    Task SynchronizeGroupedAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupResult"/> with the supplied <paramref name="argument"/> once per group. 
    /// Different groups are executed parallelly. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it, and returns its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="perGroupResult"/>.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="perGroupResult"/>.</param>
    /// <param name="perGroupResult">Factory invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>An array with all the <paramref name="perGroupResult"/> results.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult[] SynchronizeGrouped<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, TResult> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupAction"/> with the supplied <paramref name="argument"/> once per group. 
    /// Different groups are executed parallelly. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="perGroupAction"/>.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="argument">An extra argument passed to the <paramref name="perGroupAction"/>.</param>
    /// <param name="perGroupAction">Action invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void SynchronizeGrouped<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, IEnumerable<TKey>, CancellationToken> perGroupAction,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupResult"/> once per group. 
    /// Different groups are executed parallelly. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it, and returns its result.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the returned result.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="perGroupResult">Factory invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <returns>An array with all the <paramref name="perGroupResult"/> results.</returns>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    TResult[] SynchronizeGrouped<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, TResult> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull;

    /// <summary>
    /// Groups <paramref name="keys"/> by synchronizer they land on. 
    /// Executes <paramref name="perGroupAction"/> once per group. 
    /// Different groups are executed parallelly. Each group holds its own synchronizer, receives the <paramref name="keys"/> that landed on it.
    /// </summary>
    /// <typeparam name="TKey">Type of the key. Must be non-nullable.</typeparam>
    /// <param name="keys">The keys for acquiring synchronizers.</param>
    /// <param name="perGroupAction">Action invoked while holding the group's synchronizer.</param>
    /// <param name="cancellationToken">Token used to cancel waiting or execution.</param>
    /// <remarks>
    /// Only one caller for the same synchronizer will run at a time.
    /// </remarks>
    void SynchronizeGrouped<TKey>(
        IEnumerable<TKey> keys,
        Action<IEnumerable<TKey>, CancellationToken> perGroupAction,
        CancellationToken cancellationToken = default)
        where TKey : notnull;
}