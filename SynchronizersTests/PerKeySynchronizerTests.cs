namespace PerKeySynchronizersTests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PerKeySynchronizers.BoundedParallelism;

internal class PerKeySynchronizerTests
{
    private readonly IEnumerable<int> sumsToZero = Enumerable.Range(-1000, 2001);

    [Test]
    public async Task PerKeySynchronizer_Size32_CustomHashCode()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer(32);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(new IntWrapper(1), number, async (number, _) => firstSum += number, cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(new IntWrapper(2), number, async (number, _) => secondSum += number, cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstSum, Is.EqualTo(secondSum));
            Assert.That(firstSum, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task PerKeySynchronizer_Size32()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer(32);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstSum, Is.EqualTo(secondSum));
            Assert.That(firstSum, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task PerKeySynchronizer_SizeDefault()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer();

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstSum, Is.EqualTo(secondSum));
            Assert.That(firstSum, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task PerKeySynchronizer_Size1()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer(1);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Delay(1, cancellationToken);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstSum, Is.EqualTo(secondSum));
            Assert.That(firstSum, Is.EqualTo(0));
        });
    }
}
