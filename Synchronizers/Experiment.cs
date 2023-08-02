using System.Threading;

namespace Synchronizers;
public class Experiment
{
    const ulong Empty = 1ul << 63;
    const ulong NotEmpty = ~Empty;
    const uint HashMask = uint.MaxValue;
    const ulong NextMask = (ulong)(uint.MaxValue >> 1) << 32;

    // To know if entry can be reset to empty i need refCount, use some bits or add new int?
    // 7 bits is 128
    // i could use bits of NextMask
    record Entry(ulong Hash, SemaphoreSlim Semaphore);
    readonly Entry[] entries;

    public Experiment(int maxDegreeOfParallelism = 32)
    {
        entries = new Entry[maxDegreeOfParallelism];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new Entry(Empty, new SemaphoreSlim(1, 1));
        }
    }
}
