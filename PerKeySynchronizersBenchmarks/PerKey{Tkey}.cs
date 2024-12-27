namespace PerKeySynchronizersBenchmarks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using PerKeySynchronizers.UnboundedParallelism;

[RankColumn]
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PerKeyTkey
{
    private static readonly IEnumerable<int> sumsToZero = Enumerable.Range(-500, 1001).ToArray();

    [Benchmark]
    public async Task PerKeySynchronizer_OneIntKeys()
    {
        var synchronizer = new PerKeySynchronizer<int>();
        await PerKeySynchronizer_OneKeys(1, synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_OneIntWrapperKeys()
    {
        var firstKey = new IntWrapper(1);
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        await PerKeySynchronizer_OneKeys(firstKey, synchronizer, sumsToZero);
    }

    public static async Task PerKeySynchronizer_OneKeys<TSynchronizer, TKey>(
        TKey key,
        TSynchronizer synchronizer,
        IEnumerable<int> source)
        where TSynchronizer : IPerKeySynchronizer<TKey>
        where TKey : notnull
    {
        var firstSum = 0;
        await Parallel.ForEachAsync(source, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                key,
                number,
                async (number, _) =>
                {
                    await Task.Yield();
                    firstSum += number;
                },
                cancellationToken);
        });
    }

    [Benchmark]
    public async Task PerKeySynchronizer_TwoIntKeys()
    {
        var synchronizer = new PerKeySynchronizer<int>();
        await PerKeySynchronizer_TwoKeys(1, 2, synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_TwoIntWrapperKeys()
    {
        var firstKey = new IntWrapper(1);
        var secondKey = new IntWrapper(2);
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        await PerKeySynchronizer_TwoKeys(firstKey, secondKey, synchronizer, sumsToZero);
    }

    public static async Task PerKeySynchronizer_TwoKeys<TSynchronizer, TKey>(
        TKey firstKey,
        TKey secondKey,
        TSynchronizer synchronizer,
        IEnumerable<int> source)
        where TSynchronizer : IPerKeySynchronizer<TKey>
        where TKey : notnull
    {
        var firstSum = 0;
        var secondSum = 0;

        var firstSumTask = Parallel.ForEachAsync(source, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                firstKey,
                number,
                async (number, _) =>
                {
                    await Task.Yield();
                    firstSum += number;
                },
                cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(source, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                secondKey,
                number,
                async (number, _) =>
                {
                    await Task.Yield();
                    secondSum += number;
                },
                cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);
    }
}
