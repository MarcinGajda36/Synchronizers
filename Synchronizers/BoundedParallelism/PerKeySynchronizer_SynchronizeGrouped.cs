namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

public partial struct PerKeySynchronizer
{
    /// <summary>
    /// Groups the provided <paramref name="keys"/> by the internal semaphore they map to and invokes
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
    /// - The method validates the synchronizer is not disposed before proceeding.
    /// - If <paramref name="cancellationToken"/> is signaled, waiting for semaphores or delegates may throw
    ///   <see cref="OperationCanceledException"/>.
    /// </remarks>
    public readonly Task<TResult[]> SynchronizeGroupedAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var poolLength = pool_.Length;
        var keysByIndex = new Dictionary<int, List<TKey>>();
        foreach (var key in keys)
        {
            ref var group = ref CollectionsMarshal.GetValueRefOrAddDefault(keysByIndex, GetKeyIndex(key, poolLength), out var exists);
            if (exists)
            {
                group!.Add(key);
            }
            else
            {
                group = [key];
            }
        }
        var results = new Task<TResult>[keysByIndex.Count];
        var resultIdx = 0;
        foreach (var (index, group) in keysByIndex)
        {
            results[resultIdx++] = SynchronizeGroupCore(pool_[index], argument, group, perGroupResult, cancellationToken);
        }

        return Task.WhenAll(results);

        // Local helper: ensures execution yields once, then acquires the provided semaphore,
        // invokes the per-group delegate, and finally releases the semaphore.
        static async Task<TResult> SynchronizeGroupCore(
            SemaphoreSlim semaphore,
            TArgument argument,
            List<TKey> group,
            Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult,
            CancellationToken cancellationToken)
        {
            await Task.Yield();
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await perGroupResult(argument, group, cancellationToken);
            }
            finally
            {
                _ = semaphore.Release();
            }
        }
    }

    /// <summary>
    /// Groups <paramref name="keys"/> by the internal semaphore and invokes <paramref name="perGroupFunc"/> for each group.
    /// This overload accepts a per-group async action (no result) and an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's delegate.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="argument">Argument passed to each group's delegate.</param>
    /// <param name="perGroupFunc">Async action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    /// <returns>A <see cref="Task"/> that completes when all group actions complete.</returns>
    public readonly Task SynchronizeGroupedAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGroupedAsync(
            keys,
            (argument, perGroupFunc),
            static async (argumentsFuncPair, keys, cancellationToken) =>
            {
                await argumentsFuncPair.perGroupFunc(argumentsFuncPair.argument, keys, cancellationToken);
                return true;
            },
            cancellationToken);

    /// <summary>
    /// Groups <paramref name="keys"/> by the internal semaphore and invokes <paramref name="perGroupResult"/> for each group.
    /// This overload accepts a per-group async function that does not take an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupResult">Async function invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    /// <returns>A <see cref="Task{TResult[]}"/> with the results for each group.</returns>
    public readonly Task<TResult[]> SynchronizeGroupedAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, ValueTask<TResult>> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGroupedAsync(
            keys,
            perGroupResult,
            static (perGroupResult, keys, cancellationToken) => perGroupResult(keys, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Groups <paramref name="keys"/> by the internal semaphore and invokes <paramref name="perGroupFunc"/> for each group.
    /// This overload accepts a simple per-group async action without an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupFunc">Async action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    /// <returns>A <see cref="Task"/> that completes when all group actions complete.</returns>
    public readonly Task SynchronizeGroupedAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, ValueTask> perGroupFunc,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGroupedAsync(
            keys,
            perGroupFunc,
            static (perGroupFunc, keys, cancellationToken) => perGroupFunc(keys, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Synchronously groups <paramref name="keys"/> by the internal semaphore index and invokes
    /// <paramref name="perGroupResult"/> for each group while holding the group's semaphore.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's delegate.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize. Group processing uses PLINQ to execute groups in parallel.</param>
    /// <param name="argument">Argument passed to each group's delegate.</param>
    /// <param name="perGroupResult">Delegate invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    /// <returns>An array of results produced by <paramref name="perGroupResult"/> for each group.</returns>
    /// <remarks>
    /// - This synchronous variant uses PLINQ and explicit semaphore.Wait/Release around each group's execution.
    /// - Cancelling via <paramref name="cancellationToken"/> may cause <see cref="OperationCanceledException"/> to be thrown.
    /// </remarks>
    public readonly TResult[] SynchronizeGrouped<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, IEnumerable<TKey>, CancellationToken, TResult> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ValidateDispose(pool_);
        var poolLength = pool_.Length;
        var results = keys
            .AsParallel()
            .WithCancellation(cancellationToken)
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .GroupBy(
                key => GetKeyIndex(key, poolLength),
                (index, keys) =>
                {
                    var semaphore = pool_[index];
                    semaphore.Wait(cancellationToken);
                    try
                    {
                        return perGroupResult(argument, keys, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
        return results.ToArray();
    }

    /// <summary>
    /// Synchronously groups <paramref name="keys"/> and invokes <paramref name="perGroupAction"/> for each group while holding the group's semaphore.
    /// This overload accepts an <see cref="Action{TArgument, IEnumerable{TKey}, CancellationToken}"/> and an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TArgument">Type of the extra argument passed to each group's action.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="argument">Argument passed to each group's action.</param>
    /// <param name="perGroupAction">Action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    public readonly void SynchronizeGrouped<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Action<TArgument, IEnumerable<TKey>, CancellationToken> perGroupAction,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGrouped(
            keys,
            (argument, perGroupAction),
            static (argumentActionPair, keys, cancellationToken) =>
            {
                argumentActionPair.perGroupAction(argumentActionPair.argument, keys, cancellationToken);
                return true;
            },
            cancellationToken);

    /// <summary>
    /// Synchronously groups <paramref name="keys"/> by the internal semaphore index and invokes
    /// <paramref name="perGroupResult"/> for each group while holding the group's semaphore.
    /// This overload accepts a per-group function without an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <typeparam name="TResult">Type of the result produced per group.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupResult">Function invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    /// <returns>An array of results produced by <paramref name="perGroupResult"/> for each group.</returns>
    public readonly TResult[] SynchronizeGrouped<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<IEnumerable<TKey>, CancellationToken, TResult> perGroupResult,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGrouped(
            keys,
            perGroupResult,
            static (perGroupResult, keys, cancellationToken) => perGroupResult(keys, cancellationToken),
            cancellationToken);

    /// <summary>
    /// Synchronously groups <paramref name="keys"/> and invokes <paramref name="perGroupAction"/> for each group while holding the group's semaphore.
    /// This overload accepts a simple per-group action without an extra argument.
    /// </summary>
    /// <typeparam name="TKey">Type of the keys. Must be non-nullable.</typeparam>
    /// <param name="keys">Sequence of keys to group and synchronize.</param>
    /// <param name="perGroupAction">Action invoked for each group while holding the group's semaphore.</param>
    /// <param name="cancellationToken">Token to cancel waiting/processing.</param>
    public readonly void SynchronizeGrouped<TKey>(
        IEnumerable<TKey> keys,
        Action<IEnumerable<TKey>, CancellationToken> perGroupAction,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeGrouped(
            keys,
            perGroupAction,
            static (perGroupAction, keys, cancellationToken) => perGroupAction(keys, cancellationToken),
            cancellationToken);
}
