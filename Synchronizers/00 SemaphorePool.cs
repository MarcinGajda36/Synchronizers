namespace PerKeySynchronizers;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

//  abstract + generic like 'GetKeyIndex(...)' is high runtime perf cost right? I heard it uses ConcurrentDictionary<> on runtime.
public abstract class SemaphorePool
    : IDisposable
{
    private static SemaphoreSlim[] CreatePool(int size)
    {
        var pool = new SemaphoreSlim[size];
        for (var index = 0; index < pool.Length; ++index)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        return pool;
    }

    private SemaphoreSlim[] pool;

    public int Size => pool.Length;

    protected SemaphorePool(int size)
    {
        var error = ValidateSize(size);
        if (error != null)
        {
            throw error;
        }

        pool = CreatePool(size);
    }

    protected virtual Exception? ValidateSize(int size)
        => size < 1
        ? new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 1.")
        : null;

    // TODO: i want to replace abstract + generic
    // I want to try, maybe benchmark?
    // 1) Func<TKey, int>
    // 2) '0 cost abstraction' default(TKeyIndexStrategy).GetKeyIndex(TArgument argument, TKey key)
    //  this would make methods here protected and caller forwards to here with some generic?
    // 3) static abstract, just to learn
    // 4) code inlining = copy + paste
    // 5) take int indexes here, leave TKey to subclasses
    protected abstract int GetKeyIndex<TKey>(ref readonly TKey key)
        where TKey : notnull;

    public Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        CheckDispose();
        var index = GetKeyIndex(ref key);
        var semaphore = pool[index];
        return Core(semaphore, argument, func, cancellationToken);

        static async Task<TResult> Core(
            SemaphoreSlim semaphore,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> func,
            CancellationToken cancellationToken)
        {

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await func(argument, cancellationToken);
            }
            finally
            {
                _ = semaphore.Release();
            }
        }
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
        Func<CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAsync(
            key,
            func,
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

    private int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int[] keysIndexes)
        where TKey : notnull
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            var keyIndex = GetKeyIndex(in key);
            if (keysIndexes.AsSpan(..keyCount).Contains(keyIndex) is false)
            {
                keysIndexes[keyCount++] = keyIndex;
            }
            // For crazy amount of keys we can stop if keyCount == pool.Length
            // but it feels like optimizing for worst case
        }
        // We need order to avoid deadlock when:
        // 1) Thread 1 hold keys A and B
        // 2) Thread 2 waits for A and B
        // 3) Thread 3 waits for B and A
        // 4) Thread 1 releases A and B
        // 5) Thread 2 grabs A; Thread 3 grabs B
        // 6) Thread 2 waits for B; Thread 3 waits for A infinitely
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
        CheckDispose();
        return Core(this, keys, argument, resultFactory, cancellationToken);

        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (var index = locked.Length - 1; index >= 0; --index)
            {
                // I didn't saw strict need to release in reverse order, it just seemed cool
                _ = pool[locked[index]].Release();
            }
        }

        static async Task<TResult> Core(
            SemaphorePool @this,
            IEnumerable<TKey> keys,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            var pool_ = @this.pool;
            var keyIndexes = ArrayPool<int>.Shared.Rent(pool_.Length);
            var keyIndexesCount = @this.FillWithKeyIndexes(keys, keyIndexes);
            for (var index = 0; index < keyIndexesCount; ++index)
            {
                try
                {
                    await pool_[keyIndexes[index]].WaitAsync(cancellationToken);
                }
                catch
                {
                    ReleaseLocked(pool_, keyIndexes.AsSpan(..index));
                    ArrayPool<int>.Shared.Return(keyIndexes);
                    throw;
                }
            }

            try
            {
                return await resultFactory(argument, cancellationToken);
            }
            finally
            {
                ReleaseLocked(pool_, keyIndexes.AsSpan(..keyIndexesCount));
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
        CheckDispose();
        return Core(pool, argument, resultFactory, cancellationToken);

        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (var toRelease = index; toRelease >= 0; --toRelease)
            {
                _ = pool[toRelease].Release();
            }
        }

        static async Task<TResult> Core(
            SemaphoreSlim[] pool,
            TArgument argument,
            Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
            CancellationToken cancellationToken)
        {
            for (var index = 0; index < pool.Length; ++index)
            {
                try
                {
                    await pool[index].WaitAsync(cancellationToken);
                }
                catch
                {
                    ReleaseAll(pool, index - 1);
                    throw;
                }
            }

            try
            {
                return await resultFactory(argument, cancellationToken);
            }
            finally
            {
                ReleaseAll(pool, pool.Length - 1);
            }
        }
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

    protected virtual void Dispose(bool disposing)
    {
        var original = Interlocked.Exchange(ref pool!, null);
        if (original != null)
        {
            if (disposing)
            {
                Array.ForEach(original, semaphore => semaphore.Dispose());
            }
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void CheckDispose()
        => ObjectDisposedException.ThrowIf(pool == null, this);
}
