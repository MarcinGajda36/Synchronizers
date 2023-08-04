using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class Experiment
{
    const ulong Empty = 0b1000000000000000000000000000000000000000000000000000000000000000;
    const ulong HashMask = 0b0000000000000000000000000000000011111111111111111111111111111111;
    const ulong NextMask = 0b0111111111111111000000000000000000000000000000000000000000000000;
    const ulong RefCountMask = 0b0000000000000000111111111111111100000000000000000000000000000000;

    // To know if entry can be reset to empty i need refCount, use some bits or add new int?
    // 7 bits is 128
    // i could use bits of NextMask
    private sealed class Entry
    {
        public ulong Hash;
        public SemaphoreSlim Semaphore;

        public Entry(ulong hash, SemaphoreSlim semaphore)
        {
            Hash = hash;
            Semaphore = semaphore;
        }
    }

    readonly Entry[] entries;
    readonly int entriesMask;

    public Experiment(int maxDegreeOfParallelism = 32)
    {
        const int MaxMaxDegreeOfParallelism = 65535;
        if (BitOperations.IsPow2(maxDegreeOfParallelism) || maxDegreeOfParallelism > MaxMaxDegreeOfParallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} is '{MaxMaxDegreeOfParallelism}' and has to be power of 2.");
        }

        entries = new Entry[maxDegreeOfParallelism];
        entriesMask = entries.Length - 1;
        for (int i = 0; i < entries.Length; i++)
        {
            entries[i] = new Entry(Empty, new SemaphoreSlim(1, 1));
        }
    }

    public async Task<TResult> SynchronizeAsync<TKey, TArgument, TResult>(
        TKey key,
        TArgument argument,
        Func<TArgument, CancellationToken, Task<TResult>> resultFactory,
        CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        var keyHash = key.GetHashCode();
        var index = keyHash & entriesMask;
        var jumps = 0;
        while (true)
        {
            var indexMask = (index + jumps) & entriesMask;
            var entry = entries[indexMask];
            var entryHash = entry.Hash;
            if ((entryHash & Empty) != 0)
            {
                ulong newEntryHash = (ulong)keyHash + (1ul << 32);
                if (Interlocked.CompareExchange(ref entry.Hash, newEntryHash, Empty) == Empty)
                {
                    await entry.Semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await resultFactory(argument, cancellationToken);
                    }
                    finally
                    {
                        entry.Semaphore.Release();
                        // Decrement count
                    }
                }
            }
            else
            {
                // Not empty
                // On collision both the call that currently holds index and next empty space i fall back to can be contested 
                // I think i need to increment counter before looking for next empty spot to deal with contention. 
            }
        }

        return default;
    }
}
