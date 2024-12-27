namespace PerKeySynchronizersTests;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PerKeySynchronizers.BoundedParallelism;

internal class PerKeySynchronizerTests
{
    private static readonly IEnumerable<int> sumsToZero = Enumerable.Range(-50_000, 100_001).ToArray();

    [Test]
    public async Task PerKeySynchronizer_Size32_CustomObject()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer(32);
        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                new IntWrapper(1),
                async _ =>
                {
                    await Task.Yield();
                    firstSum += number;
                },
                cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                new IntWrapper(2),
                async _ =>
                {
                    await Task.Yield();
                    secondSum += number;
                },
                cancellationToken);
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
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                1,
                async _ =>
                {
                    await Task.Yield();
                    firstSum += number;
                },
                cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                2,
                async _ =>
                {
                    await Task.Yield();
                    secondSum += number;
                },
                cancellationToken);
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
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                1,
                async _ =>
                {
                    await Task.Yield();
                    firstSum += number;
                },
                cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                2,
                async _ =>
                {
                    await Task.Yield();
                    secondSum += number;
                },
                cancellationToken);
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
        var totalSum = 0;

        var synchronizer = new PerKeySynchronizer(1);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                1,
                async _ =>
                {
                    await Task.Yield();
                    secondSum += number;
                    totalSum += number;
                },
                cancellationToken);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, cancellationToken) =>
        {
            await Task.Yield();
            await synchronizer.SynchronizeAsync(
                2,
                async _ =>
                {
                    await Task.Yield();
                    secondSum += number;
                    totalSum += number;
                },
                cancellationToken);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.Multiple(() =>
        {
            Assert.That(firstSum, Is.EqualTo(secondSum));
            Assert.That(totalSum, Is.EqualTo(firstSum));
            Assert.That(firstSum, Is.EqualTo(0));
        });
    }
}
