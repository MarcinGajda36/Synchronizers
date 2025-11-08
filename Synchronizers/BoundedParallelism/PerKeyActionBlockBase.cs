namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public abstract class PerKeyActionBlockBase<TMessage>
    : IDisposable
{
    private readonly ActionBlock<TMessage>[] actionBlocks;
    private readonly CancellationTokenSource cancellationSource;
    private bool disposedValue;

    public Task Completion { get; }
    public int ActionBlocksCount => actionBlocks.Length;

    protected PerKeyActionBlockBase(
        object initializeState,
        ExecutionDataflowBlockOptions? perActionBlockOptions = null)
    {
        ExecutionDataflowBlockOptions optionsWithLinkedToken;
        if (perActionBlockOptions is { } notNull)
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
        actionBlocks = InitializeBlocks(initializeState, optionsWithLinkedToken);
        Completion = CreateCompletion(actionBlocks, cancellationSource);
    }

    protected abstract ActionBlock<TMessage>[] InitializeBlocks(object initializeState, ExecutionDataflowBlockOptions options);

    private static async Task CreateCompletion(ActionBlock<TMessage>[] flows, CancellationTokenSource cancellationSource)
    {
        var completions = Array.ConvertAll(flows, flow => flow.Completion);
        var first = await Task.WhenAny(completions);
        await cancellationSource.CancelAsync();
        await Task.WhenAll([first, .. completions]);
    }

    private void ValidateIndex(int index)
    {
        if (index < 0 || index >= actionBlocks.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index), $"Index has to be between 0 inclusive and {nameof(ActionBlocksCount)}:{ActionBlocksCount} exclusive.");
        }
    }

    /// <summary>
    /// Enqueues an item to one of <see cref="ActionBlock{TMessage}"/>.
    /// </summary>
    /// <param name="index">Index to specific <see cref="ActionBlock{TMessage}"/> that will receive the message. 
    /// Has be between 0 inclusive and <see cref="ActionBlocksCount"/> exclusive.</param>
    /// <param name="toEnqueue">Message to put onto <see cref="ActionBlock{TMessage}"/> queue.</param>
    /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
    public bool Enqueue(int index, TMessage toEnqueue)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        ValidateIndex(index);
        return actionBlocks[index].Post(toEnqueue);
    }

    /// <summary>
    /// Enqueues an item to one of <see cref="ActionBlock{TMessage}"/>.
    /// </summary>
    /// <param name="index">Index to specific <see cref="ActionBlock{TMessage}"/> that will receive the message. 
    /// Has be between 0 inclusive and <see cref="ActionBlocksCount"/> exclusive.</param>
    /// <param name="toEnqueue">Message to put onto <see cref="ActionBlock{TMessage}"/> queue.</param>
    /// <param name="cancellationToken">Cancellation of enqueuing.</param>
    /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
    public Task<bool> EnqueueAsync(int index, TMessage toEnqueue, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        ValidateIndex(index);
        return actionBlocks[index].SendAsync(toEnqueue, cancellationToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetKeyIndex<TKey>(TKey key)
        where TKey : notnull
        => key.GetHashCode() % actionBlocks.Length;

    /// <summary>
    /// Enqueues an item to one of <see cref="ActionBlock{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of key used for deriving an index.</typeparam>
    /// <param name="key">Key used for deriving index to specific <see cref="ActionBlock{TMessage}"/>.</param>
    /// <param name="toEnqueue">Message to put onto <see cref="ActionBlock{TMessage}"/> queue.</param>
    /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
    public bool Enqueue<TKey>(TKey key, TMessage toEnqueue)
        where TKey : notnull
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        ArgumentNullException.ThrowIfNull(key);
        return actionBlocks[GetKeyIndex(key)].Post(toEnqueue);
    }

    /// <summary>
    /// Enqueues an item to one of <see cref="ActionBlock{TMessage}"/>.
    /// </summary>
    /// <typeparam name="TKey">Type of key used for deriving an index.</typeparam>
    /// <param name="key">Key used for deriving index to specific <see cref="ActionBlock{TMessage}"/>.</param>
    /// <param name="toEnqueue">Message to put onto <see cref="ActionBlock{TMessage}"/> queue.</param>
    /// <param name="cancellationToken">Cancellation of enqueuing.</param>
    /// <returns>true if the item was accepted by the target block; otherwise, false.</returns>
    public Task<bool> EnqueueAsync<TKey>(TKey key, TMessage toEnqueue, CancellationToken cancellationToken = default)
        where TKey : notnull
    {
        ObjectDisposedException.ThrowIf(disposedValue, this);
        ArgumentNullException.ThrowIfNull(key);
        return actionBlocks[GetKeyIndex(key)].SendAsync(toEnqueue, cancellationToken);
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
