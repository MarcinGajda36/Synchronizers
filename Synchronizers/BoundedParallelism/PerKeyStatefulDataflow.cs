namespace Synchronizers.BoundedParallelism; // Not worth breaking change

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyStatefulDataflow<TState, TMessage>
    : IDisposable
{
    private readonly ActionBlock<TMessage>[] flows;
    private readonly CancellationTokenSource cancellationSource;

    public int FlowsCount => flows.Length;
    public Task Completion { get; }

    public PerKeyStatefulDataflow(
        int flowsCount,
        Func<TState> initialStateFactory,
        Func<TState, TMessage, CancellationToken, ValueTask<TState>> flowAction,
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
            var flowState = initialStateFactory();
            flows[idx] = new ActionBlock<TMessage>(
                async message => flowState = await flowAction(flowState, message, linkedToken),
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArgumentOutOfRangeException? ValidateIndex(int flowIndex)
        => flowIndex < 0 || flowIndex >= FlowsCount
            ? new(nameof(flowIndex), $"Index to be between 0 inclusive and FlowsCount:{FlowsCount} exclusive.")
            : null;

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public bool Enqueue(int flowIndex, TMessage toEnqueue)
    {
        if (ValidateIndex(flowIndex) is { } exception)
        {
            throw exception;
        }
        return flows[flowIndex].Post(toEnqueue);
    }

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public Task<bool> EnqueueAsync(int flowIndex, TMessage toEnqueue, CancellationToken cancellationToken = default)
    {
        if (ValidateIndex(flowIndex) is { } exception)
        {
            throw exception;
        }
        return flows[flowIndex].SendAsync(toEnqueue, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull
        => key.GetHashCode() % FlowsCount;

    public bool Enqueue<TKey>(TKey flowKey, TMessage toEnqueue)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(flowKey);
        return Enqueue(GetKeyIndex(flowKey), toEnqueue);
    }

    public Task<bool> EnqueueAsync<TKey>(TKey flowKey, TMessage toEnqueue, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(flowKey);
        return EnqueueAsync(GetKeyIndex(flowKey), toEnqueue, cancellationToken);
    }

    public void Dispose()
        => cancellationSource.Dispose();
}
