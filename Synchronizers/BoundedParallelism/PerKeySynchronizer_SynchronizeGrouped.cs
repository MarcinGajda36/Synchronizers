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
    /// Groups keys by semaphore they would land on and invokes all groups asynchronously.
    /// </summary>
    /// <returns>Result per synchronization group.</returns>
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
