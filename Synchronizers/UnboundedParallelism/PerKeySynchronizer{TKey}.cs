namespace PerKeySynchronizers.UnboundedParallelism;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

internal struct SemaphoreCountPair(SemaphoreSlim semaphore, int count)
{
    public SemaphoreSlim Semaphore = semaphore;
    public int Count = count;
}

/// <summary>
/// Synchronizes operations so all operation on given key happen one at a time, 
/// while allowing operations for different keys to happen in parallel.
/// Uses ConcurrentDictionary<TKey, ...> to grab semaphore for given key.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <param name="equalityComparer">
/// Comparer used to determine if keys are the same.
/// </param>
public readonly struct PerKeySynchronizer<TKey>(IEqualityComparer<TKey>? equalityComparer = null)
    : IPerKeySynchronizer<TKey>, IEquatable<PerKeySynchronizer<TKey>>
    where TKey : notnull
{
    public PerKeySynchronizer() : this(null) { }

    private readonly Dictionary<TKey, SemaphoreCountPair> semaphores = new(equalityComparer);

    private static SemaphoreSlim GetOrCreate(Dictionary<TKey, SemaphoreCountPair> semaphores, TKey key)
    {
        lock (semaphores)
        {
            ref var old = ref CollectionsMarshal.GetValueRefOrAddDefault(semaphores, key, out var exists);
            ++old.Count;
            var semaphore = old.Semaphore ??= new SemaphoreSlim(1, 1);
            return semaphore;
        }
    }

    private static void Cleanup(Dictionary<TKey, SemaphoreCountPair> semaphores, TKey key)
    {
        lock (semaphores)
        {
            ref var old = ref CollectionsMarshal.GetValueRefOrNullRef(semaphores, key);
            if ((--old.Count) == 0)
            {
                old.Semaphore.Dispose();
                _ = semaphores.Remove(key);
            }
        }
    }

    public async readonly ValueTask<TResult> SynchronizeAsync<TArgument, TResult>(
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

    public async ValueTask SynchronizeAsync<TArgument>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
    {
        var semaphores_ = semaphores;
        var semaphore = GetOrCreate(semaphores_, key);
        try
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await func(argument, cancellationToken);
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

    public ValueTask<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    public ValueTask SynchronizeAsync(
        TKey key,
        Func<CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(key, func, static (func, token) => func(token), cancellationToken);

    public TResult Synchronize<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
    {
        var semaphores_ = semaphores;
        var semaphore = GetOrCreate(semaphores_, key);
        try
        {
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
        finally
        {
            Cleanup(semaphores_, key);
        }
    }

    public void Synchronize<TArgument>(
        TKey key,
        TArgument argument,
        Action<TArgument, CancellationToken> action,
        CancellationToken cancellationToken = default)
    {
        var semaphores_ = semaphores;
        var semaphore = GetOrCreate(semaphores_, key);
        try
        {
            semaphore.Wait(cancellationToken);
            try
            {
                action(argument, cancellationToken);
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

    public TResult Synchronize<TResult>(
        TKey key,
        Func<CancellationToken, TResult> resultFactory,
        CancellationToken cancellationToken = default)
        => Synchronize(key, resultFactory, static (resultFactory, token) => resultFactory(token), cancellationToken);

    public void Synchronize(
        TKey key,
        Action<CancellationToken> action,
        CancellationToken cancellationToken = default)
        => Synchronize(key, action, static (action, token) => action(token), cancellationToken);

    public readonly bool Equals(PerKeySynchronizer<TKey> other)
        => ReferenceEquals(semaphores, other.semaphores);
    public override readonly bool Equals(object? obj)
        => obj is PerKeySynchronizer<TKey> other && Equals(other);
    public static bool operator ==(PerKeySynchronizer<TKey> left, PerKeySynchronizer<TKey> right)
        => left.Equals(right);
    public static bool operator !=(PerKeySynchronizer<TKey> left, PerKeySynchronizer<TKey> right)
        => !left.Equals(right);
    public override readonly int GetHashCode()
        => semaphores.GetHashCode();
}