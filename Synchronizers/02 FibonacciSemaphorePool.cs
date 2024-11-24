namespace Synchronizers;

using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Most complex code out of pools.
/// Perf of finding index seems comparable to modulo.
/// Pool size restricted to powers of 2.
/// Total concurrency is limited to pool size.
/// 
/// Fibonacci hash seems nice for hash distribution right?
/// </summary>
public sealed class FibonacciSemaphorePool : IDisposable
{
    private readonly SemaphoreSlim[] pool;
    private readonly int poolIndexBitShift;

    public FibonacciSemaphorePool(uint size)
    {
        if (size < 2 || BitOperations.IsPow2(size) is false)
        {
            throw new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be bigger then 1 and a power of 2.");
        }

        poolIndexBitShift = 32 - BitOperations.TrailingZeroCount(size);
        pool = new SemaphoreSlim[size];
        for (var index = 0; index < pool.Length; index++)
        {
            pool[index] = new SemaphoreSlim(1, 1);
        }
    }

    //Program.<<Main>$>g__GetKeyIndex|0_2(System.Guid)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: imul eax, 0x9e3779b9
    //L0011: shr eax, 0x1b
    //L0014: ret
    private int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull
    {
        const uint Fibonacci = 2654435769u; // 2 ^ 32 / PHI
        unchecked
        {
            var keyHash = (uint)key.GetHashCode();
            var fibonacciHash = keyHash * Fibonacci;
            var index = fibonacciHash >> poolIndexBitShift;
            return (int)index;
        }
    }

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

    public void Dispose()
        => Array.ForEach(pool, semaphore => semaphore.Dispose());
}
