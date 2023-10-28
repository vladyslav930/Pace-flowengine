using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class UnsafeFinallyFlowSpace<TFlowData, TSagaContext> : UnsafeFlowSpace<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public List<Exception> MainFlowExceptions { get; internal set; }
        public List<Exception> FinallyFlowExceptions { get; internal set; }
        internal UnsafeFinallyFlowSpace() { }

        internal static UnsafeFinallyFlowSpace<TFlowData, TSagaContext> Create(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            IFlowSagaContextRepository repository,
            string rabbitMqConnectionString)
        {
            var flowInstance = ctx.Instance;

            var space = new UnsafeFinallyFlowSpace<TFlowData, TSagaContext>
            {
                RabbitMqConnectionString = rabbitMqConnectionString,
                BehaviorContext = ctx,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                Data = flowInstance.FlowData,
                MainFlowExceptions = flowInstance.FlowContainer
                    .GetMainFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList(),
                FinallyFlowExceptions = flowInstance.FlowContainer
                    .GetFinallyFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList()
            };

            space.SagaContext = new Lazy<TSagaContext>(() =>
            {
                return FlowSagaContextUtil.CreateSagaContext<TSagaContext>(space.CorrelationId, repository);
            });

            return space;
        }
    }
}
