using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Synchronizers.BoundedParallelism;

public sealed class PerKeyDataflow<TMessage>
    : IDisposable
{
    private readonly ActionBlock<TMessage>[] flows;
    private readonly CancellationTokenSource cancellationSource;

    public int FlowsCount => flows.Length;
    public Task Completion { get; }

    public PerKeyDataflow(
        int flowsCount,
        Func<TMessage, CancellationToken, Task> flowAction,
        ExecutionDataflowBlockOptions? perFlowOptions = null)
    {
        ExecutionDataflowBlockOptions optionsWithLinkedToken;
        if (perFlowOptions is { } notNull)
        {
            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(notNull.CancellationToken);
            optionsWithLinkedToken = new()
            {
                BoundedCapacity = notNull.BoundedCapacity,
                CancellationToken = cancellationSource.Token,
                EnsureOrdered = notNull.EnsureOrdered,
                MaxDegreeOfParallelism = notNull.MaxDegreeOfParallelism,
                MaxMessagesPerTask = notNull.MaxDegreeOfParallelism,
                NameFormat = notNull.NameFormat,
                SingleProducerConstrained = notNull.SingleProducerConstrained,
                TaskScheduler = notNull.TaskScheduler
            };
        }
        else
        {
            cancellationSource = new();
            optionsWithLinkedToken = new() { CancellationToken = cancellationSource.Token };
        }
        var linkedToken = cancellationSource.Token;
        flows = new ActionBlock<TMessage>[flowsCount];
        for (var idx = 0; idx < flows.Length; idx++)
        {
            flows[idx] = new ActionBlock<TMessage>(
                message => flowAction(message, linkedToken),
                optionsWithLinkedToken);
        }
        Completion = CreateCompletion(flows, cancellationSource);
    }

    private static async Task CreateCompletion(ActionBlock<TMessage>[] flows, CancellationTokenSource cancellationSource)
    {
        var flowsCompletion = Array.ConvertAll(flows, flow => flow.Completion);
        var first = await Task.WhenAny(flowsCompletion);
        await cancellationSource.CancelAsync();
        await Task.WhenAll([first, .. flowsCompletion]);
    }

    public ArgumentOutOfRangeException? ValidateKey(int flowIndex)
        => flowIndex < 0 || flowIndex >= FlowsCount
            ? new(nameof(flowIndex), $"Index to be between 0 inclusive and FlowsCount:{FlowsCount} exclusive.")
            : null;

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public bool Enqueue(int flowIndex, TMessage toEnqueue)
    {
        if (ValidateKey(flowIndex) is { } exception)
        {
            throw exception;
        }
        return flows[flowIndex].Post(toEnqueue);
    }

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public Task<bool> EnqueueAsync(int flowIndex, TMessage toEnqueue, CancellationToken cancellationToken = default)
    {
        if (ValidateKey(flowIndex) is { } exception)
        {
            throw exception;
        }
        return flows[flowIndex].SendAsync(toEnqueue, cancellationToken);
    }

    public void Dispose()
        => cancellationSource.Dispose();
}
