﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;

// Old but kept for sentimental value 

/// <summary>
/// Very complex code.
/// High overhead of semaphore finding.
/// Unbounded parallelism with minimal contention.
/// Total control of TKey equality comparison.
/// Restricted to one type of TKey per instance.
/// </summary>
public sealed class ConcurrentDictionaryRefCountDisposable<TKey>
    where TKey : notnull
{
    private sealed class Synchronizer : IDisposable
    {
        public readonly struct Lease : IDisposable
        {
            private readonly SemaphoreSlim? toRelease;
            private readonly IDisposable refCount;

            public readonly bool IsAcquired;

            public Lease(bool isAcquired, IDisposable refCount, SemaphoreSlim? toRelease)
            {
                IsAcquired = isAcquired;
                this.toRelease = toRelease;
                this.refCount = refCount;
            }

            public void Dispose()
            {
                toRelease?.Release();
                refCount.Dispose();
            }
        }

        private readonly RefCountDisposable refCountDisposable;
        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
        private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers;
        private readonly TKey key;

        public bool AddedToDictionary { get; set; }

        public Synchronizer(ConcurrentDictionary<TKey, Synchronizer> synchronizers, TKey key)
        {
            this.synchronizers = synchronizers;
            this.key = key;

            var disposable = Disposable.Create(this, static @this =>
            {
                if (@this.AddedToDictionary)
                {
                    @this.synchronizers.TryRemove(@this.key, out _);
                }
                @this.semaphoreSlim.Dispose();
            });
            refCountDisposable = new RefCountDisposable(disposable);
        }

        public async ValueTask<Lease> AcquireAsync(CancellationToken cancellationToken)
        {
            var refCount = refCountDisposable.GetDisposable();
            if (refCountDisposable.IsDisposed)
            {
                return new Lease(false, refCount, null);
            }
            else
            {
                try
                {
                    await semaphoreSlim.WaitAsync(cancellationToken);
                }
                catch
                {
                    refCount.Dispose();
                    throw;
                }
                return new Lease(true, refCount, semaphoreSlim);
            }
        }

        public void Dispose()
            => refCountDisposable.Dispose();
    }

    private readonly ConcurrentDictionary<TKey, Synchronizer> synchronizers;

    public ConcurrentDictionaryRefCountDisposable(IEqualityComparer<TKey>? equalityComparer = null)
        => synchronizers = new(equalityComparer);

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (synchronizers.TryGetValue(key, out var oldSynchronizer))
            {
                using var lease = await oldSynchronizer.AcquireAsync(cancellationToken);
                if (lease.IsAcquired)
                {
                    return await resultFactory(argument, cancellationToken);
                }
            }
            else
            {
                using var newSynchronizer = new Synchronizer(synchronizers, key);
                if (synchronizers.TryAdd(key, newSynchronizer))
                {
                    newSynchronizer.AddedToDictionary = true;
                    using var lease = await newSynchronizer.AcquireAsync(cancellationToken);
                    return await resultFactory(argument, cancellationToken);
                }
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public Task<TResult> SynchronizeAsync<TResult>(
        TKey key,
        Func<CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        => SynchronizeAsync(
            key,
            resultFactory,
            static (factory, cancellation) => factory(cancellation),
            cancellationToken);
}