namespace Synchronizers.BoundedParallelism;

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyDataflow<TMessage>(
    int flowsCount,
    Func<TMessage, CancellationToken, Task> flowAction,
    ExecutionDataflowBlockOptions? perFlowOptions = null)
    : PerKeyDataflowBase<TMessage>(CreateInitializationFactory(flowsCount, flowAction), perFlowOptions)
{
    private static Func<ExecutionDataflowBlockOptions, ActionBlock<TMessage>[]> CreateInitializationFactory(
        int flowsCount,
        Func<TMessage, CancellationToken, Task> flowAction)
            => options =>
            {
                var token = options.CancellationToken;
                var flows = new ActionBlock<TMessage>[flowsCount];
                for (var idx = 0; idx < flows.Length; idx++)
                {
                    flows[idx] = new ActionBlock<TMessage>(
                        message => flowAction(message, token),
                        options);
                }
                return flows;
            };
}
