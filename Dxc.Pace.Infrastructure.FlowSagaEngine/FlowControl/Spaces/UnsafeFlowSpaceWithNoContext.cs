using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class UnsafeFlowSpaceWithNoContext<TFlowData, TSagaContext> 
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public Guid CorrelationId { get; internal set; }
        public Guid? UserId { get; internal set; }
        public string UserEmail { get; internal set; }
        public TFlowData Data { get; internal set; }
        public string RabbitMqConnectionString { get; internal set; }
        public BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaStartCommand<TFlowData>> BehaviorContext { get; internal set; }

        internal UnsafeFlowSpaceWithNoContext() { }

        internal static UnsafeFlowSpaceWithNoContext<TFlowData, TSagaContext> Create(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaStartCommand<TFlowData>> ctx,
            string rabbitMqConnectionString)
        {
            var flowInstance = ctx.Instance;

            var space = new UnsafeFlowSpaceWithNoContext<TFlowData, TSagaContext>
            {
                RabbitMqConnectionString = rabbitMqConnectionString,
                BehaviorContext = ctx,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                Data = flowInstance.FlowData,
            };

            return space;
        }
    }
}
