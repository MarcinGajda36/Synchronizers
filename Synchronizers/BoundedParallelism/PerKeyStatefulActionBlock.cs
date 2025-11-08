namespace PerKeySynchronizers.BoundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyStatefulActionBlock<TState, TMessage>(
    int actionBlocksCount,
    Func<TState> initialPerBlockStateFactory,
    Func<TState, TMessage, CancellationToken, ValueTask<TState>> actionBlockAction,
    ExecutionDataflowBlockOptions? perActionBlockOptions = null)
    : PerKeyActionBlockBase<TMessage>(new InitializeState(actionBlocksCount, initialPerBlockStateFactory, actionBlockAction), perActionBlockOptions)
{
    private sealed record InitializeState(
        int ActionBlocksCount,
        Func<TState> InitialPerBlockStateFactory,
        Func<TState, TMessage, CancellationToken, ValueTask<TState>> ActionBlockAction);

    protected override ActionBlock<TMessage>[] InitializeBlocks(object initializeState, ExecutionDataflowBlockOptions options)
    {
        var (actionBlocksCount, initialPerBlockStateFactory, actionBlockAction) = (InitializeState)initializeState;
        var token = options.CancellationToken;
        var actionBlocks = new ActionBlock<TMessage>[actionBlocksCount];
        for (var idx = 0; idx < actionBlocks.Length; idx++)
        {
            var state = initialPerBlockStateFactory();
            actionBlocks[idx] = new ActionBlock<TMessage>(
                async message => state = await actionBlockAction(state, message, token),
                options);
        }
        return actionBlocks;
    }
}
