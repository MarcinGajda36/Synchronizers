namespace Synchronizers;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

//  abstract + generic like 'GetKeyIndex(...)' is high runtime perf cost right? I heard it uses ConcurrentDictionaryOfSemaphores<> on runtime.
public abstract class SemaphorePool : IDisposable
{
    private readonly SemaphoreSlim[] pool;
    private bool disposedValue;

    public int Size { get; }

    protected SemaphorePool(int size)
    {
        var error = ValidateSize(size);
        if (error != null)
        {
            throw error;
        }

        pool = CreatePool(size);
        Size = size;
    }

    protected virtual Exception? ValidateSize(int size)
        => size < 1
        ? new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 1.")
        : null;

    private static SemaphoreSlim[] CreatePool(int size)
    {
        var pool = new SemaphoreSlim[size];
        for (var index = 0; index < pool.Length; ++index)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
        return pool;
    }

    protected abstract int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull;

    public async Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        CheckDispose();
        var index = GetKeyIndex(key);
        var semaphore = pool[index];
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

    private int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int[] keysIndexes)
        where TKey : notnull
    {
        var keyCount = 0;
        foreach (var key in keys)
        {
            var keyIndex = GetKeyIndex(key);
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

    public async Task<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (var index = locked.Length - 1; index >= 0; --index)
            {
                // I didn't saw strict need to release in reverse order, it just seemed cool
                _ = pool[locked[index]].Release();
            }
        }

        CheckDispose();
        var pool_ = pool;
        var keyIndexes = ArrayPool<int>.Shared.Rent(pool_.Length);
        var keyIndexesCount = FillWithKeyIndexes(keys, keyIndexes);
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

    public async Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (var toRelease = index; toRelease >= 0; --toRelease)
            {
                _ = pool[toRelease].Release();
            }
        }

        CheckDispose();
        var pool_ = pool;
        for (var index = 0; index < pool_.Length; ++index)
        {
            try
            {
                await pool_[index].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseAll(pool_, index - 1);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseAll(pool_, pool_.Length - 1);
        }
    }

    public Task SynchronizeAllAsync<TKey, TArgument>(
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
        => SynchronizeAllAsync(
            (argument, func),
            static async (arguments, cancellationToken) =>
            {
                await arguments.func(arguments.argument, cancellationToken);
                return true;
            },
            cancellationToken);

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Array.ForEach(pool, semaphore => semaphore.Dispose());
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void CheckDispose()
        => ObjectDisposedException.ThrowIf(disposedValue, this);
}
