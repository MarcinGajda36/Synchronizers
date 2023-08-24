using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Synchronizers;
public class Experiment
{
    const ulong Empty = 0b1000000000000000000000000000000000000000000000000000000000000000; // Is that needed?
    const ulong HashMask = 0b0000000000000000000000000000000011111111111111111111111111111111;
    const ulong NextMask = 0b0111111111111111000000000000000000000000000000000000000000000000;
    const ulong RefCountMask = 0b0000000000000000111111111111111100000000000000000000000000000000;

    readonly ulong[] keys;
    readonly SemaphoreSlim[] semaphores;
    readonly int keysIndexMask;

    public Experiment(int maxDegreeOfParallelism = 32)
    {
        const int MaxMaxDegreeOfParallelism = 65535;
        if (BitOperations.IsPow2(maxDegreeOfParallelism) || maxDegreeOfParallelism > MaxMaxDegreeOfParallelism)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), $"{nameof(maxDegreeOfParallelism)} is '{MaxMaxDegreeOfParallelism}' and has to be power of 2.");
        }

        keys = new ulong[maxDegreeOfParallelism];
        semaphores = new SemaphoreSlim[maxDegreeOfParallelism];
        keysIndexMask = keys.Length - 1;
        for (int idx = 0; idx < semaphores.Length; idx++)
        {
            semaphores[idx] = new SemaphoreSlim(1, 1);
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
        var initialIndex = keyHash & keysIndexMask;
        var jumps = 0;
        while (true)
        {
            var currentIndex = (initialIndex + jumps) & keysIndexMask; // Masks wraps around
            var indexKey = keys[currentIndex];
            if ((indexKey & Empty) != 0)
            {
                ulong refCount = 1ul << 32;
                ulong newEntryHash = (ulong)keyHash + refCount;
                if (Interlocked.CompareExchange(ref keys[currentIndex], newEntryHash, Empty) == Empty)
                {
                    var semaphore = semaphores[currentIndex];
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await resultFactory(argument, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                        // Decrement count
                    }
                }
                else
                {
                    // It was empty but someone reserved it first.
                    continue;
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
