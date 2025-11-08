namespace Synchronizers.BoundedParallelism; // Not worth breaking change

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Please use PerKeySynchronizers.BoundedParallelism.PerKeyStatefulActionBlock instead")]
public sealed class PerKeyStatefulDataflow<TState, TMessage>(
    int flowsCount,
    Func<TState> initialStateFactory,
    Func<TState, TMessage, CancellationToken, ValueTask<TState>> flowAction,
    ExecutionDataflowBlockOptions? perFlowOptions = null)
    : PerKeyDataflowBase<TMessage>(new InitializeState(flowsCount, initialStateFactory, flowAction), perFlowOptions)
{
    private sealed record InitializeState(
        int FlowsCount,
        Func<TState> InitialStateFactory,
        Func<TState, TMessage, CancellationToken, ValueTask<TState>> FlowAction);

    protected override ActionBlock<TMessage>[] InitializeFlows(object state, ExecutionDataflowBlockOptions options)
    {
        var (flowsCount, initialStateFactory, flowsAction) = (InitializeState)state;
        var token = options.CancellationToken;
        var flows = new ActionBlock<TMessage>[flowsCount];
        for (var idx = 0; idx < flows.Length; idx++)
        {
            var flowState = initialStateFactory();
            flows[idx] = new ActionBlock<TMessage>(
                async message => flowState = await flowAction(flowState, message, token),
                options);
        }
        return flows;
    }
}
