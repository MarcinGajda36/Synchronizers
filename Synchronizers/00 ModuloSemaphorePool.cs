using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;

/// <summary>
/// As simple code as it gets.
/// Modulo for pool index is already cheap but could be a even cheaper.
/// No restrictions on pool size.
/// Concurrency is limited to pool size.
/// 
/// Best for simplicity.
/// </summary>
public sealed class SemaphorePool : IDisposable
{
    private readonly SemaphoreSlim[] pool;

    public SemaphorePool(int size)
    {
        if (size < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 2.");
        }

        pool = new SemaphoreSlim[size];
        for (int index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
    }

    //Program.<<Main>$>g__GetKeyIndex|0_0(System.Guid, <>c__DisplayClass0_0 ByRef)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: mov rcx, [rdx]
    //L000e: cdq
    //L000f: idiv dword ptr[rcx + 8]
    //L0012: mov eax, edx
    //L0014: ret
    private int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull
        => key.GetHashCode() % pool.Length;

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

    public async Task<TResult> SynchronizeManyAsync<TKey, TArgument, TResult>(
        IEnumerable<TKey> keys,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        static void ReleaseLocked(SemaphoreSlim[] pool, Stack<int> locked)
        {
            while (locked.TryPop(out var toRelease))
            {
                pool[toRelease].Release();
            }
        }

        var indexes = new SortedSet<int>();
        foreach (var key in keys)
        {
            indexes.Add(GetKeyIndex(key));
        }

        var locked = new Stack<int>(indexes.Count);
        foreach (int toLock in indexes)
        {
            try
            {
                await pool[toLock].WaitAsync(cancellationToken);
                locked.Push(toLock);
            }
            catch
            {
                ReleaseLocked(pool, locked);
                throw;
            }
        }

        try
        {
            return await resultFactory(argument, cancellationToken);
        }
        finally
        {
            ReleaseLocked(pool, locked);
        }
    }

    public void Dispose()
        => Array.ForEach(pool, semaphore => semaphore.Dispose());
}
