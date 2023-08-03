using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class Experiment
{
    const ulong Empty           = 0b1000000000000000000000000000000000000000000000000000000000000000;
    const ulong HashMask        = 0b0000000000000000000000000000000011111111111111111111111111111111;
    const ulong NextMask        = 0b0111111111111111000000000000000000000000000000000000000000000000;
    const ulong RefCountMask    = 0b0000000000000000111111111111111100000000000000000000000000000000;

    // To know if entry can be reset to empty i need refCount, use some bits or add new int?
    // 7 bits is 128
    // i could use bits of NextMask
    record Entry(ulong Hash, SemaphoreSlim Semaphore);
    readonly Entry[] entries;

    public Experiment(int maxDegreeOfParallelism = 32)
    {
        const int MaxMaxDegreeOfParallelism = 65535;
        if (maxDegreeOfParallelism > MaxMaxDegreeOfParallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} is '{MaxMaxDegreeOfParallelism}'.");
        }
        entries = new Entry[maxDegreeOfParallelism];
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new Entry(Empty, new SemaphoreSlim(1, 1));
        }
    }

    public async Task<TResult> SynchronizeAsync<TArgument, TResult>(
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
    {
        return default;
    }
}
