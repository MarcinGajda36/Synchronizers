namespace PerKeySynchronizers;

using System;
using System.Numerics;

/// <summary>
/// Total concurrency is limited to pool size.
/// Lowes perf overhead of finding key semaphore i can think of.
/// It may be most susceptible to hash conflicts.
/// </summary>
public sealed class BitMaskSemaphorePool(int size = 32) : SemaphorePool(size)
{
    private readonly int poolIndexBitMask = size - 1;

    protected override Exception? ValidateSize(int size)
        => size < 1 || BitOperations.IsPow2(size) is false
        ? new ArgumentOutOfRangeException(nameof(size), size, "Pool size has to be at least 1 and a power of 2.")
        : null;

    //Program.<<Main>$>g__GetKeyIndex|0_1(System.Guid)
    //L0000: mov eax, [rcx]
    //L0002: xor eax, [rcx + 4]
    //L0005: xor eax, [rcx + 8]
    //L0008: xor eax, [rcx + 0xc]
    //L000b: and eax, 0x1f
    //L000e: ret
    protected override int GetKeyIndex<TKey>(TKey key)
        => key.GetHashCode() & poolIndexBitMask;

}
