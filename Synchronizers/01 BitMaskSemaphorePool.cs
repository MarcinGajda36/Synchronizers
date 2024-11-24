namespace Synchronizers;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Still simple code, but a little more complex then modulo.
/// Lowes perf overhead of finding key semaphore i can think of.
/// Pool size restricted to powers of 2.
/// Total concurrency is limited to pool size.
/// 
/// It may be most susceptible to hash conflicts? 
/// </summary>
public sealed class BitMaskSemaphorePool : IDisposable
{
    private readonly SemaphoreSlim[] pool;
    private readonly int poolIndexBitMask;

    public BitMaskSemaphorePool(uint size)
    {
        if (size < 2 || BitOperations.IsPow2(size) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be bigger then 1 and a power of 2.");
        }

        poolIndexBitMask = (int)size - 1;
        pool = new SemaphoreSlim[size];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
    }

    //Program.<<Main>$>g__GetKeyIndex|0_1(System.Guid)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: and eax, 0x1f
    //L000e: ret
    private int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull
        => key.GetHashCode() & poolIndexBitMask;

    public async Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> func,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var index = GetKeyIndex(key);
        var semaphore = pool[index];
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await func(argument, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async ValueTask<TResult> SynchronizeValueAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, ValueTask<TResult>> func,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        var index = GetKeyIndex(key);
        var semaphore = pool[index];
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            return await func(argument, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private int FillWithKeyIndexes<TKey>(IEnumerable<TKey> keys, int[] keysIndexes)
        where TKey : notnull
    {
        int keyCount = 0;
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
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Span<int> locked)
        {
            for (int index = locked.Length - 1; index >= 0; index--)
            {
                // I didn't saw strict need to release in reverse order, it just seemed cool
                pool[locked[index]].Release();
            }
        }

        var keyIndexes = ArrayPool<int>.Shared.Rent(pool.Length);
        int keyIndexesCount = FillWithKeyIndexes(keys, keyIndexes);

        for (int index = 0; index < keyIndexesCount; index++)
        {
            try
            {
                await pool[keyIndexes[index]].WaitAsync(cancellationToken);
            }
            catch
            {
                ReleaseLocked(pool, keyIndexes.AsSpan(..index));
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
            ReleaseLocked(pool, keyIndexes.AsSpan(..keyIndexesCount));
            ArrayPool<int>.Shared.Return(keyIndexes);
        }
    }

    public async Task<TResult> SynchronizeAllAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        static void ReleaseAll(SemaphoreSlim[] pool, int index)
        {
            for (int toRelease = index; toRelease >= 0; toRelease--)
            {
                pool[toRelease].Release();
            }
        }

        for (int index = 0; index < pool.Length; index++)
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

    public void Dispose()
        => Array.ForEach(pool, semaphore => semaphore.Dispose());
}
