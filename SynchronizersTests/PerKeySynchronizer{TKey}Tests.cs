﻿namespace PerKeySynchronizersTests;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PerKeySynchronizers.UnboundedParallelism;

internal class PerKeySynchronizer_TKey_Tests
{
    private static readonly IEnumerable<int> sumsToZero = Enumerable.Range(-50_000, 100_001).ToArray();

    [Test]
    public async Task PerKeySynchronizer_TwoKeys()
    {
        var firstSum = 0;
        var secondSum = 0;

        var synchronizer = new PerKeySynchronizer<int>();

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
    public async Task SynchronizeAsync_SingleKey()
    {
        // Arrange
        var synchronizer = new PerKeySynchronizer<int>();
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
        var synchronizer = new PerKeySynchronizer<int>();
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

        await Task.WhenAll(task1.AsTask(), task2.AsTask(), task3.AsTask());

        // Assert
        Assert.That(executionOrder, Is.EqualTo("231"));
    }

    [Test]
    public async Task SynchronizeAsync_MultipleKeys_AllExecuteConcurrently_CustomObject()
    {
        // Arrange
        var synchronizer = new PerKeySynchronizer<IntWrapper>();
        var argument = "test";
        var executionOrder = "";

        // Act
        var task1 = synchronizer.SynchronizeAsync(new IntWrapper(1), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(100, cancellationToken);
            executionOrder += "1";
        });

        var task2 = synchronizer.SynchronizeAsync(new IntWrapper(2), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            executionOrder += "2";
        });

        var task3 = synchronizer.SynchronizeAsync(new IntWrapper(3), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(75, cancellationToken);
            executionOrder += "3";
        });

        await Task.WhenAll(task1.AsTask(), task2.AsTask(), task3.AsTask());

        // Assert
        Assert.That(executionOrder, Is.EqualTo("231"));
    }

    [Test]
    public async Task SynchronizeAsync_MultipleKeys_AllExecuteConcurrently_CustomObject_AndEqualityComparer()
    {
        // Arrange
        var synchronizer = new PerKeySynchronizer<IntWrapper>(EqualityComparer<IntWrapper>.Default);
        var argument = "test";
        var executionOrder = "";

        // Act
        var task1 = synchronizer.SynchronizeAsync(new IntWrapper(1), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(100, cancellationToken);
            executionOrder += "1";
        });

        var task2 = synchronizer.SynchronizeAsync(new IntWrapper(2), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            executionOrder += "2";
        });

        var task3 = synchronizer.SynchronizeAsync(new IntWrapper(3), argument, async (arg, cancellationToken) =>
        {
            await Task.Delay(75, cancellationToken);
            executionOrder += "3";
        });

        await Task.WhenAll(task1.AsTask(), task2.AsTask(), task3.AsTask());

        // Assert
        Assert.That(executionOrder, Is.EqualTo("231"));
    }

    [Test]
    public void SynchronizeAsync_CancellationRequested_DoesNotExecuteFunction()
    {
        // Arrange
        var synchronizer = new PerKeySynchronizer<int>();
        var isFunctionExecuted = false;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Multiple(() =>
        {
            _ = Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                _ = await synchronizer.SynchronizeAsync(
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
