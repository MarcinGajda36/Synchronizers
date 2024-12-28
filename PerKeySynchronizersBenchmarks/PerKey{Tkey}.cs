namespace PerKeySynchronizersBenchmarks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using PerKeySynchronizers.UnboundedParallelism;

[RankColumn]
[MemoryDiagnoser]
public class PerKeyTkey
{
    private static readonly int[] sumsToZero = Enumerable.Range(-50_000, 100_001).ToArray();

    [Benchmark]
    public async Task PerKeySynchronizer_OneIntKeys()
    {
        var synchronizer = new PerKeySynchronizer<int>();
        await PerKeySynchronizerWork([1], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_OneGuidKeys()
    {
        var synchronizer = new PerKeySynchronizer<Guid>();
        await PerKeySynchronizerWork([new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1)], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_OneIntWrapperKeys()
    {
        var firstKey = new IntWrapper(1);
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        await PerKeySynchronizerWork([firstKey], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_TwoIntKeys()
    {
        var synchronizer = new PerKeySynchronizer<int>();
        await PerKeySynchronizerWork([1, 2], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_TwoGuidKeys()
    {
        var synchronizer = new PerKeySynchronizer<Guid>();
        await PerKeySynchronizerWork(
            [new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1), new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2)],
            synchronizer,
            sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_TwoIntWrapperKeys()
    {
        var firstKey = new IntWrapper(1);
        var secondKey = new IntWrapper(2);
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        await PerKeySynchronizerWork([firstKey, secondKey], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_FourIntKeys()
    {
        var synchronizer = new PerKeySynchronizer<int>();
        await PerKeySynchronizerWork([1, 2, 3, 4], synchronizer, sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_FourGuidKeys()
    {
        var synchronizer = new PerKeySynchronizer<Guid>();
        await PerKeySynchronizerWork(
            [
                new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1),
                new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2),
                new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3),
                new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 4)
            ],
            synchronizer,
            sumsToZero);
    }

    [Benchmark]
    public async Task PerKeySynchronizer_FourIntWrapperKeys()
    {
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        await PerKeySynchronizerWork(
            [new IntWrapper(1), new IntWrapper(2), new IntWrapper(3), new IntWrapper(4)],
            synchronizer,
            sumsToZero);
    }

    public static async Task PerKeySynchronizerWork<TSynchronizer, TKey>(
        TKey[] keys,
        TSynchronizer synchronizer,
        IEnumerable<int> source)
        where TSynchronizer : IPerKeySynchronizer<TKey>
        where TKey : notnull
    {
        var tasks = new Task[keys.Length];
        for (var i = 0; i < keys.Length; ++i)
        {
            var key = keys[i];
            var sum = 0;
            tasks[i] = Parallel.ForEachAsync(source, async (number, cancellationToken) =>
            {
                await Task.Yield();
                await synchronizer.SynchronizeAsync(
                    key,
                    number,
                    async (number, _) =>
                    {
                        await Task.Yield();
                        sum += number;
                    },
                    cancellationToken);
            });
        }
        await Task.WhenAll(tasks);
    }
}
