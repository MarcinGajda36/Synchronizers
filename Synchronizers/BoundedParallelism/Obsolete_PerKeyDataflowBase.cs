namespace Synchronizers.BoundedParallelism; // Not worth breaking change

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Please use PerKeySynchronizers.BoundedParallelism.PerKeyActionBlockBase instead")]
public abstract class PerKeyDataflowBase<TMessage>
    : IDisposable
{
    protected readonly CancellationTokenSource cancellationSource;
    private readonly ActionBlock<TMessage>[] flows;
    private bool disposedValue;

    public int FlowsCount => flows.Length;
    public Task Completion { get; }

    protected PerKeyDataflowBase(
        object initializeState,
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
        flows = InitializeFlows(initializeState, optionsWithLinkedToken);
        Completion = CreateCompletion(flows, cancellationSource);
    }

    protected abstract ActionBlock<TMessage>[] InitializeFlows(object initializeState, ExecutionDataflowBlockOptions options);

    private static async Task CreateCompletion(ActionBlock<TMessage>[] flows, CancellationTokenSource cancellationSource)
    {
        var flowsCompletion = Array.ConvertAll(flows, flow => flow.Completion);
        var first = await Task.WhenAny(flowsCompletion);
        await cancellationSource.CancelAsync();
        await Task.WhenAll([first, .. flowsCompletion]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ArgumentOutOfRangeException? ValidateIndex(int flowIndex)
        => flowIndex < 0 || flowIndex >= FlowsCount
            ? new(nameof(flowIndex), $"Index to be between 0 inclusive and FlowsCount:{FlowsCount} exclusive.")
            : null;

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public bool Enqueue(int flowIndex, TMessage toEnqueue)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        if (ValidateIndex(flowIndex) is { } exception)
        {
            throw exception;
        }
        return flows[flowIndex].Post(toEnqueue);
    }

    /// <param name="flowIndex">Index to be between 0 inclusive and FlowsCount exclusive.</param>
    public Task<bool> EnqueueAsync(int flowIndex, TMessage toEnqueue, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                cancellationSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
