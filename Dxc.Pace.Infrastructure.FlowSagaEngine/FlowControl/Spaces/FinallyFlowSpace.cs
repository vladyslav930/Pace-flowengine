using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class FinallyFlowSpace<TFlowData, TSagaContext> : FlowSpace<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public List<Exception> MainFlowExceptions { get; internal set; }

        internal FinallyFlowSpace() { }
    }

    public class FinallyFlowSpace<TFlowData, TSagaContext, TResponse> : FlowSpace<TFlowData, TSagaContext, TResponse>
        where TFlowData : class, new()
        where TSagaContext : class
        where TResponse : class
    {
        public List<Exception> MainFlowExceptions { get; internal set; }

        internal FinallyFlowSpace() { }

        internal static FinallyFlowSpace<TFlowData, TSagaContext, TResponse> Create(
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            IFlowSagaContextRepository repository,
            FlowResponse flowResponse)
        {
            var space = new FinallyFlowSpace<TFlowData, TSagaContext, TResponse>
            {
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                Data = flowInstance.FlowData
            };
            space.SagaContext = new Lazy<TSagaContext>(() => FlowSagaContextUtil.CreateSagaContext<TSagaContext>(
                space.CorrelationId, repository));
            space.MainFlowExceptions = flowInstance.FlowContainer
                .GetMainFlowExceptions()
                .Select(x =>x.Exception)
                .ToList();

            if (!string.IsNullOrEmpty(flowResponse?.JsonResult))
            {
                space.Response = JsonConvert.DeserializeObject<TResponse>(flowResponse.JsonResult);
            }
            return space;
        }
    }

}
