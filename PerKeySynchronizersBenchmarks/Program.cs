namespace PerKeySynchronizersBenchmarks;

using BenchmarkDotNet.Running;

internal class Program
{
    private static void Main(string[] args)
        => _ = BenchmarkRunner.Run<PerKeyTkey>();
}
