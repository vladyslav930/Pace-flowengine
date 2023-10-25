using System.Diagnostics;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators
{
    public class FlowCommunicatorFactory<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {

        internal FlowCommunicatorFactory() { }

        public FlowMonolog<TFlowData, TSagaContext, TRequest> Monolog<TRequest>(string name = null)
            where TRequest : class
        {
            var monolog = new FlowMonolog<TFlowData, TSagaContext, TRequest>()
            {
                Name = name,
                CallerMethodName = GetCallerMethodName()
            };
            return monolog;
        }

        public FlowDialog<TFlowData, TSagaContext, TRequest, TResponse> Dialog<TRequest, TResponse>(string name = null)
            where TRequest : class
            where TResponse : class
        {

            var dialog = new FlowDialog<TFlowData, TSagaContext, TRequest, TResponse>()
            {
                Name = name,
                CallerMethodName = GetCallerMethodName()
            };
            return dialog;
        }

        public ChildFlowSaga<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> ChildSaga<TChildFlowData, TChildSagaContext>(string name = null)
            where TChildFlowData : class, new()
            where TChildSagaContext : class
        {
            var childSaga = new ChildFlowSaga<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>()
            {
                Name = name,
                CallerMethodName = GetCallerMethodName()
            };
            return childSaga;
        }

        private string GetCallerMethodName()
        {
            return new StackTrace().GetFrame(2).GetMethod().Name;
        }
    }
}
