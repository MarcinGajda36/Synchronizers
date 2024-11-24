namespace Synchronizers;

using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

public class Experiment
{
    const ulong NextMask
        = 0b1111111111111111000000000000000000000000000000000000000000000000;
    const ulong RefCountMask
        = 0b0000000000000000111111111111111100000000000000000000000000000000;
    const ulong HashMask
        = 0b0000000000000000000000000000000011111111111111111111111111111111;
    const ulong AllButRefCountMask
        = 0b1111111111111111000000000000000011111111111111111111111111111111;

    static readonly int RefCountMaskTrailingZeros = BitOperations.TrailingZeroCount(RefCountMask);
    static readonly int NextMaskTrailingZeros = BitOperations.TrailingZeroCount(NextMask);

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
        for (var idx = 0; idx < semaphores.Length; idx++)
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
        var jumps = 0; // i think i need to inc ref count every jump and remove next only on decrement to 0?
        while (true)
        {
            var currentIndex = (initialIndex + jumps) & keysIndexMask;
            var indexKey = keys[currentIndex];
            if (indexKey == 0) // I always need to increment ref count and maintain hash even when initial hasher freed count, or i would let same key to access 2 semaphores.
            {
                // We found empty spot.
                var refCountOne = 1ul << RefCountMaskTrailingZeros;
                var newEntryKey = (ulong)keyHash + refCountOne; // I can make it OR right?
                if (Interlocked.CompareExchange(ref keys[currentIndex], newEntryKey, indexKey) == indexKey)
                {
                    // We reserved the spot.
                    var semaphore = semaphores[currentIndex];
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await resultFactory(argument, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                        // Decrement count, assuming ref count is still 1 and indexKey hasn't been changed.
                        var decremented = newEntryKey & NextMask;
                        indexKey = Interlocked.CompareExchange(ref keys[currentIndex], decremented, newEntryKey);
                        if (indexKey != newEntryKey)
                        {
                            // Failed to decrement, ref count and indexKey can be anything (CompareExchange could fail because of Next).
                            while (true)
                            {
                                var indexKeyCopy = indexKey;
                                var decrementedRefCount = ((indexKeyCopy & RefCountMask) >> RefCountMaskTrailingZeros) - 1;
                                decremented = decrementedRefCount == 0ul
                                    ? 0ul
                                    : (indexKeyCopy & AllButRefCountMask) | (decrementedRefCount << RefCountMaskTrailingZeros);
                                indexKey = Interlocked.CompareExchange(ref keys[currentIndex], decremented, indexKeyCopy);
                                if (indexKey == indexKeyCopy)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // All good.
                        }
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
