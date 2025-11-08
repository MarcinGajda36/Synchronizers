namespace Synchronizers.BoundedParallelism; // Not worth breaking change

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyDataflow<TMessage>(
    int flowsCount,
    Func<TMessage, CancellationToken, Task> flowAction,
    ExecutionDataflowBlockOptions? perFlowOptions = null)
    : PerKeyDataflowBase<TMessage>(new InitializeState(flowsCount, flowAction), perFlowOptions)
{
    private sealed record InitializeState(
        int FlowsCount,
        Func<TMessage, CancellationToken, Task> FlowAction);

    protected override ActionBlock<TMessage>[] InitializeFlows(object state, ExecutionDataflowBlockOptions options)
    {
        var (flowsCount, flowsAction) = (InitializeState)state;
        var token = options.CancellationToken;
        var flows = new ActionBlock<TMessage>[flowsCount];
        for (var idx = 0; idx < flows.Length; idx++)
        {
            flows[idx] = new ActionBlock<TMessage>(
                message => flowAction(message, token),
                options);
        }
        return flows;
    }
}
