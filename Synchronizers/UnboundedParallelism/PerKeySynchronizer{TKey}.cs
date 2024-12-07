﻿namespace PerKeySynchronizers.UnboundedParallelism;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Synchronizes operations so all operation on given key happen one at a time, 
/// while allowing operations for different keys to happen in parallel.
/// Uses ConcurrentDictionary<TKey, ...> to grab semaphore for given key.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <param name="equalityComparer">
/// Comparer used to determine if keys are the same.
/// </param>
public sealed class PerKeySynchronizer<TKey>(IEqualityComparer<TKey>? equalityComparer = null)
    : IPerKeySynchronizer<TKey>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CountSemaphorePair> semaphores = new(equalityComparer);
    private readonly record struct CountSemaphorePair(int Count, SemaphoreSlim Semaphore);

    private static SemaphoreSlim GetOrCreate(ConcurrentDictionary<TKey, CountSemaphorePair> semaphores, TKey key)
    {
        while (true)
        {
            if (semaphores.TryGetValue(key, out var old))
            {
                var incremented = old with { Count = old.Count + 1 };
                if (semaphores.TryUpdate(key, incremented, old))
                {
                    return old.Semaphore;
                }
            }
            else
            {
                var @new = new CountSemaphorePair(1, new SemaphoreSlim(1, 1));
                if (semaphores.TryAdd(key, @new))
                {
                    return @new.Semaphore;
                }
                else
                {
                    @new.Semaphore.Dispose();
                }
            }
        }
    }

    private static void Cleanup(ConcurrentDictionary<TKey, CountSemaphorePair> semaphores, TKey key)
    {
        while (true)
        {
            var old = semaphores[key];
            if (old.Count == 1)
            {
                var toRemove = KeyValuePair.Create(key, old);
                if (semaphores.TryRemove(toRemove))
                {
                    old.Semaphore.Dispose();
                    return;
                }
            }
            else
            {
                var decremented = old with { Count = old.Count - 1 };
                if (semaphores.TryUpdate(key, decremented, old))
                {
                    return;
                }
            }
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {

        var semaphores_ = semaphores;
        var semaphore = GetOrCreate(semaphores_, key);
        try
        {
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
        finally
        {
            Cleanup(semaphores_, key);
        }
    }

    public Task SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    public Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            resultFactory,
            static (func, token) => func(token),
            cancellationToken);

    public Task SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            func,
            static async (func, token) =>
            {
                await func(token);
                return true;
            },
            cancellationToken);
}