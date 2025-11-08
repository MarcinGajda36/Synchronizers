namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyActionBlock<TMessage>(
    int actionBlocksCount,
    Func<TMessage, CancellationToken, Task> actionBlockAction,
    ExecutionDataflowBlockOptions? perActionBlockOptions = null)
    : PerKeyActionBlockBase<TMessage>(new InitializeState(actionBlocksCount, actionBlockAction), perActionBlockOptions)
{
    private sealed record InitializeState(
        int ActionBlocksCount,
        Func<TMessage, CancellationToken, Task> ActionBlockAction);

    protected override ActionBlock<TMessage>[] InitializeBlocks(object initializeState, ExecutionDataflowBlockOptions options)
    {
        var (actionBlocksCount, actionBlockAction) = (InitializeState)initializeState;
        var actionBlocks = new ActionBlock<TMessage>[actionBlocksCount];
        var token = options.CancellationToken;
        for (var idx = 0; idx < actionBlocks.Length; idx++)
        {
            actionBlocks[idx] = new ActionBlock<TMessage>(
                message => actionBlockAction(message, token),
                options);
        }
        return actionBlocks;
    }
}
