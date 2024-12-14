﻿namespace PerKeySynchronizers.BoundedParallelism;
using System;
using System.Threading;
using System.Threading.Tasks;

public partial class PerKeySynchronizer
{
    public Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static async Task<TResult> Core(
            SemaphoreSlim[] pool,
            TKey key,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var index = GetKeyIndex(key, pool.Length);
            var semaphore = pool[index];
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

        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        return Core(pool_, key, argument, resultFactory, cancellationToken);
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

    public TResult Synchronize<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var pool_ = pool;
        ObjectDisposedException.ThrowIf(pool_ == null, this);
        var index = GetKeyIndex(key, pool.Length);
        var semaphore = pool[index];
        semaphore.Wait(cancellationToken);
        try
        {
            return resultFactory(argument, cancellationToken);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public void Synchronize<TKey, TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(
            key,
            (argument, action),
            static (arguments, token) =>
            {
                arguments.action(arguments.argument, token);
                return true;
            },
            cancellationToken);

    public TResult Synchronize<TKey, TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(
            key,
            resultFactory,
            static (factory, token) => factory(token),
            cancellationToken);

    public void Synchronize<TKey>(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => Synchronize(
            key,
            action,
            (action, token) =>
            {
                action(token);
                return true;
            },
            cancellationToken);
}
