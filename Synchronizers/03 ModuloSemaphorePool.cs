namespace PerKeySynchronizers;

/// <summary>
/// Total concurrency is limited to pool size.
/// No restrictions on pool size, but prime numbers are the best for hash distribution.
/// </summary>
public sealed class ModuloSemaphorePool(int size = 31)
    : SemaphorePool(size)
{
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
    protected override int GetKeyIndex<TKey>(ref readonly TKey key)
        => key.GetHashCode() % Size;

}
