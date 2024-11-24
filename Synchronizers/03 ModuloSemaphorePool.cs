namespace Synchronizers;
/// <summary>
/// As simple code as it gets.
/// Modulo for pool index is already cheap but could be a even cheaper with bit operations.
/// Total concurrency is limited to pool size.
/// No restrictions on pool size.
/// 
/// Maybe i should change it to modulo prime like ConcurrentDictionary? 
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
    protected override int GetKeyIndex<TKey>(TKey key)
        => key.GetHashCode() % Size;

}
