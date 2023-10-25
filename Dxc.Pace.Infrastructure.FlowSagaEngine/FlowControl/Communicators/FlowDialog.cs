using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators
{
    public class FlowDialog<TFlowData, TSagaContext, TRequest, TResponse> : FlowMonolog<TFlowData, TSagaContext, TRequest>
        where TFlowData : class, new()
        where TSagaContext : class
        where TRequest : class
        where TResponse : class
    {
        internal FlowDialog() : base()
        {
        }

        internal FlowDialog(FlowMonolog<TFlowData, TSagaContext, TRequest> monolog)
            : base()
        {
            Name = monolog.Name;
            CallerMethodName = monolog.CallerMethodName;
            RequestFactories = monolog.RequestFactories;
            SendCondition = monolog.SendCondition;
            WaitCondition = monolog.WaitCondition;
        }

        internal CompleteDelegate<TFlowData, TSagaContext> CompleteProcessor { get; set; }

        internal ChunkResponseDelegate<TFlowData, TSagaContext, TResponse> ChunkResponseProcessor { get; set; }

        public void SetCompleteProcessor(Func<FlowSpace<TFlowData, TSagaContext>, Task> func)
        {
            CompleteProcessor = async s => { await func(s); };
        }

        public void SetCompleteProcessor(Action<FlowSpace<TFlowData, TSagaContext>> func)
        {
            CompleteProcessor = s =>
            {
                func(s);
                return Task.CompletedTask;
            };
        }

        public void SetChunkResponseProcessor(Func<FlowSpace<TFlowData, TSagaContext, TResponse>, int, Task> func)
        {
            ChunkResponseProcessor = async (s, chunkIndex) => { await func(s, chunkIndex); };
        }

        public void SetChunkResponseProcessor(Action<FlowSpace<TFlowData, TSagaContext, TResponse>, int> func)
        {
            ChunkResponseProcessor = (s, chunkIndex) =>
            {
                func(s, chunkIndex);
                return Task.CompletedTask;
            };
        }
    }
}
