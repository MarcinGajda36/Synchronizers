using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Synchronizers;

namespace SynchronizersTests;

public class Tests
{
    private readonly IEnumerable<int> sumsToZero = Enumerable.Range(-500, 1001);

    [Test]
    public async Task SemaphorePool()
    {
        int firstSum = 0;
        int secondSum = 0;

        var synchronizer = new SemaphorePool(69);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, _);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, _);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }

    [Test]
    public async Task BitMaskSemaphorePool()
    {
        int firstSum = 0;
        int secondSum = 0;

        var synchronizer = new BitMaskSemaphorePool(32);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, _);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, _);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }

    [Test]
    public async Task FibonacciSemaphorePool()
    {
        int firstSum = 0;
        int secondSum = 0;

        var synchronizer = new FibonacciSemaphorePool(32);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, _);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, _);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }

    [Test]
    public async Task DictionaryOfSemaphores()
    {
        int firstSum = 0;
        int secondSum = 0;

        var synchronizer = new DictionaryOfSemaphores<int>();

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, _);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, _);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }

    [Test]
    public async Task ConcurrentDictionaryOfSemaphores()
    {
        int firstSum = 0;
        int secondSum = 0;

        var synchronizer = new ConcurrentDictionaryOfSemaphores<int>();

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(1, number, async (number, _) => firstSum += number, _);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (number, _) =>
        {
            await Task.Delay(1, _);
            await synchronizer.SynchronizeAsync(2, number, async (number, _) => secondSum += number, _);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }
}