#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Synchronizers.BoundedParallelism; // Not worth breaking change
#pragma warning restore IDE0130 // Namespace does not match folder structure

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("Please use PerKeySynchronizers.BoundedParallelism.PerKeyActionBlock instead")]
public sealed class PerKeyDataflow<TMessage>(
    int flowsCount,
    Func<TMessage, CancellationToken, Task> flowAction,
    ExecutionDataflowBlockOptions? perFlowOptions = null)
    : PerKeyDataflowBase<TMessage>(new InitializeState(flowsCount, flowAction), perFlowOptions)
{
    private sealed record InitializeState(
        int FlowsCount,
        Func<TMessage, CancellationToken, Task> FlowAction);

    protected override ActionBlock<TMessage>[] InitializeFlows(object initializeState, ExecutionDataflowBlockOptions options)
    {
        var (flowsCount, flowsAction) = (InitializeState)initializeState;
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
