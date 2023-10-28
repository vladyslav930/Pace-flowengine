using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class UnsafeFlowSpace<TFlowData, TSagaContext> : FlowSpace<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public string RabbitMqConnectionString { get; internal set; }
        public BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> BehaviorContext { get; internal set; }

        internal UnsafeFlowSpace() { }

        public async Task SendWithFaultAddressAsync<TMessage>(ISendEndpoint endpoint, TMessage message, Uri faultAddress = null)
        {
            faultAddress ??= FlowQueueUtil.GetUnifiedFaultAddress(this.RabbitMqConnectionString);

            await endpoint.Send(message, c =>
            {
                c.FaultAddress = faultAddress;
            });
        }
    }
}
