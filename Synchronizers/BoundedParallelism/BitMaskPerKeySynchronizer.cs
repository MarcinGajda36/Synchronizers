﻿namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Total concurrency is limited to pool size.
/// Lowes perf overhead of finding key semaphore i can think of.
/// It may be most susceptible to hash conflicts.
/// </summary>
public sealed class BitMaskPerKeySynchronizer
    : IPerKeySynchronizer, IDisposable
{
    private SemaphoreSlim[] pool;
    private readonly int mask;

    public BitMaskPerKeySynchronizer(int size = 32)
    {
        var error = ValidateSize(size);
        if (error != null)
        {
            throw error;
        }

        pool = SemaphorePool.CreatePool(size);
        mask = size - 1;
    }

    private static ArgumentOutOfRangeException? ValidateSize(int size)
        => size < 1 || BitOperations.IsPow2(size) is false
        ? new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 1 and a power of 2.")
        : null;

    //Program.<<Main>$>g__GetKeyIndex|0_1(System.Guid)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: and eax, 0x1f
    //L000e: ret
    private static int GetKeyIndex<TKey>(ref readonly TKey key, int mask)
        where TKey : notnull
        => key.GetHashCode() & mask;

    public Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        var index = GetKeyIndex(ref key, mask);
        var semaphore = pool_[index];
        return SemaphorePool.SynchronizeAsync(semaphore, argument, resultFactory, cancellationToken);
    }

    public Task SynchronizeAsync<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAsync<TKey, TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            resultFactory,
            static (func, token) => func(token),
            cancellationToken);

    public Task SynchronizeAsync<TKey>(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            func,
            static async (func, token) =>
            {
                await func(token);
                return true;
            },
            cancellationToken);

    private static int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int mask, int[] keysIndexes)
        where TKey : notnull
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            var index = GetKeyIndex(in key, mask);
            if (keysIndexes.AsSpan(..keyCount).Contains(index) is false)
            {
                keysIndexes[keyCount++] = index;
            }
        }

        Array.Sort(keysIndexes, 0, keyCount);
        return keyCount;
    }

    public Task<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return Core(keys, argument, resultFactory, pool_, mask, cancellationToken);

        static async Task<TResult> Core(
            IEnumerable<TKey> keys,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            SemaphoreSlim[] pool,
            int mask,
            CancellationToken cancellationToken)
        {
            var keyIndexes = ArrayPool<int>.Shared.Rent(pool.Length);
            var keyIndexesCount = FillWithKeyIndexes(keys, mask, keyIndexes);
            try
            {
                return await SemaphorePool.SynchronizeManyAsync(pool, keyIndexes, keyIndexesCount, argument, resultFactory, cancellationToken);
            }
            finally
            {
                ArrayPool<int>.Shared.Return(keyIndexes);
            }
        }
    }

    public Task SynchronizeManyAsync<TKey, TArgument>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeManyAsync<TKey, TResult>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public Task SynchronizeManyAsync<TKey>(
        IEnumerable<TKey> keys,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeManyAsync(
            keys,
            func,
            static async (func, cancellationToken) =>
            {
                await func(cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return SemaphorePool.SynchronizeAllAsync(pool_, argument, resultFactory, cancellationToken);
    }

    public Task SynchronizeAllAsync<TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAllAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            resultFactory,
            static (resultFactory, cancellationToken) => resultFactory(cancellationToken),
            cancellationToken);

    public Task SynchronizeAllAsync(
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAllAsync(
            func,
            static async (func, cancellationToken) =>
            {
                await func(cancellationToken);
                return true;
            },
            cancellationToken);

    public void Dispose()
    {
        var original = Interlocked.Exchange(ref pool!, null);
        if (original != null)
        {
            Array.ForEach(original, semaphore => semaphore.Dispose());
        }
    }
}
