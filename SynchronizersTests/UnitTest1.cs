using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    [Test]
    public async Task SynchronizeAsync_SingleKey_ExecutesFunction()
    {
        // Arrange
        var synchronizer = new DictionaryOfSemaphores<int>();
        var key = 1;
        var argument = "test";
        var isFunctionExecuted = false;

        // Act
        await synchronizer.SynchronizeAsync(key, argument, async (arg, cancellationToken) =>
        {
            isFunctionExecuted = true;
            await Task.Delay(100, cancellationToken); // Simulate some async work
        });

        // Assert
        Assert.That(isFunctionExecuted, Is.True);
    }

    [Test]
    public async Task SynchronizeAsync_MultipleKeys_AllExecuteConcurrently()
    {
        // Arrange
        var synchronizer = new DictionaryOfSemaphores<int>();
        var argument = "test";
        var executionOrder = "";

        // Act
        var task1 = synchronizer.SynchronizeAsync(1, argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(100, cancellationToken);
            executionOrder += "1";
        });

        var task2 = synchronizer.SynchronizeAsync(2, argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            executionOrder += "2";
        });

        var task3 = synchronizer.SynchronizeAsync(3, argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(75, cancellationToken);
            executionOrder += "3";
        });

        await Task.WhenAll(task1, task2, task3);

        // Assert
        Assert.That(executionOrder, Is.EqualTo("231"));
    }

    [Test]
    public void SynchronizeAsync_CancellationRequested_DoesNotExecuteFunction()
    {
        // Arrange
        var synchronizer = new DictionaryOfSemaphores<int>();
        var isFunctionExecuted = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await synchronizer.SynchronizeAsync(
                    1,
                    "_",
                    async (_, cancellationToken) =>
                    {
                        await Task.Delay(100, cancellationToken);
                        isFunctionExecuted = true;
                        return isFunctionExecuted;
                    },
                    cancellationTokenSource.Token);
            });

            Assert.That(isFunctionExecuted, Is.False);
        });
    }
}