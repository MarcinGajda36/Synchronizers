namespace PerKeySynchronizers.UnboundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IPerKeySynchronizer<TKey>
    where TKey : notnull
{
    /// <summary>
    /// Acquire synchronization for the specified <paramref name="key"/>, execute an asynchronous factory with the supplied <paramref name="argument"/>,
    /// and return its result. Only one caller for the same <typeparamref name="TKey"/> value will run concurrently.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the result produced by <paramref name="resultFactory"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="argument">Argument passed to <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Asynchronous factory executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the factory result.</returns>
    ValueTask<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire synchronization for the specified <paramref name="key"/> and execute an asynchronous action with the supplied <paramref name="argument"/>.
    /// Only one caller for the same <typeparamref name="TKey"/> value will run concurrently.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="func"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="argument">Argument passed to <paramref name="func"/>.</param>
    /// <param name="func">Asynchronous action executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the action finishes.</returns>
    ValueTask SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire synchronization for the specified <paramref name="key"/>, execute an asynchronous factory that does not take an extra argument,
    /// and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the result produced by <paramref name="resultFactory"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="resultFactory">Asynchronous factory executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> that completes with the factory result.</returns>
    ValueTask<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Acquire synchronization for the specified <paramref name="key"/> and execute an asynchronous action that does not take an extra argument.
    /// </summary>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="func">Asynchronous action executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the action finishes.</returns>
    ValueTask SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously acquire synchronization for the specified <paramref name="key"/>, execute <paramref name="resultFactory"/>, and return its result.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="resultFactory"/>.</typeparam>
    /// <typeparam name="TResult">Type of the result produced by <paramref name="resultFactory"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="argument">Argument passed to <paramref name="resultFactory"/>.</param>
    /// <param name="resultFactory">Factory executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>The result produced by <paramref name="resultFactory"/>.</returns>
    TResult Synchronize<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously acquire synchronization for the specified <paramref name="key"/> and execute <paramref name="action"/>.
    /// </summary>
    /// <typeparam name="TArgument">Type of the extra argument passed to <paramref name="action"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="argument">Argument passed to <paramref name="action"/>.</param>
    /// <param name="action">Action executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    void Synchronize<TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously acquire synchronization for the specified <paramref name="key"/>, execute <paramref name="resultFactory"/> (no extra argument), and return its result.
    /// </summary>
    /// <typeparam name="TResult">Type of the result produced by <paramref name="resultFactory"/>.</typeparam>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="resultFactory">Factory executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    /// <returns>The result produced by <paramref name="resultFactory"/>.</returns>
    TResult Synchronize<TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Synchronously acquire synchronization for the specified <paramref name="key"/> and execute <paramref name="action"/> (no extra argument).
    /// </summary>
    /// <param name="key">The key to synchronize on.</param>
    /// <param name="action">Action executed while holding the key's synchronization.</param>
    /// <param name="cancellationToken">Token to cancel waiting or execution.</param>
    void Synchronize(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default);
}