namespace Synchronizers;

using System;
using System.Numerics;

/// <summary>
/// Total concurrency is limited to pool size.
/// Fibonacci hash seems nice for balance of hash distribution and perf.
/// </summary>
public sealed class FibonacciSemaphorePool(int size = 32) : SemaphorePool(size)
{
    private readonly int poolIndexBitShift = 32 - BitOperations.TrailingZeroCount(size);

    protected override Exception? ValidateSize(int size)
        => size < 1 || BitOperations.IsPow2(size) is false
        ? new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 1 and a power of 2.")
        : null;

    //Program.<<Main>$>g__GetKeyIndex|0_2(System.Guid)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: imul eax, 0x9e3779b9
    //L0011: shr eax, 0x1b
    //L0014: ret
    protected override int GetKeyIndex<TKey>(TKey key)
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
}
