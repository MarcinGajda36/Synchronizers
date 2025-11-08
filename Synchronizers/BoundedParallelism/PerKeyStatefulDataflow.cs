namespace Synchronizers.BoundedParallelism; // Not worth breaking change

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

public sealed class PerKeyStatefulDataflow<TState, TMessage>(
    int flowsCount,
    Func<TState> initialStateFactory,
    Func<TState, TMessage, CancellationToken, ValueTask<TState>> flowAction,
    ExecutionDataflowBlockOptions? perFlowOptions = null)
    : PerKeyDataflowBase<TMessage>(CreateInitializationFactory(flowsCount, initialStateFactory, flowAction), perFlowOptions)
{
    private static Func<ExecutionDataflowBlockOptions, ActionBlock<TMessage>[]> CreateInitializationFactory(
        int flowsCount,
        Func<TState> initialStateFactory,
        Func<TState, TMessage, CancellationToken, ValueTask<TState>> flowAction)
            => options =>
            {
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
            };
}
