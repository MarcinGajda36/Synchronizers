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

        var synchronizer = new SemaphorePool(10);

        var firstSumTask = Parallel.ForEachAsync(sumsToZero, async (numer, _) =>
        {
            await Task.Delay(1);
            await synchronizer.SynchronizeAsync(1, numer, async (number, _) => firstSum += numer);
        });
        var secondSumTask = Parallel.ForEachAsync(sumsToZero, async (numer, _) =>
        {
            await Task.Delay(1);
            await synchronizer.SynchronizeAsync(2, numer, async (number, _) => secondSum += numer);
        });
        await Task.WhenAll(firstSumTask, secondSumTask);

        Assert.That(firstSum, Is.EqualTo(secondSum));
        Assert.That(firstSum, Is.EqualTo(0));
    }
}