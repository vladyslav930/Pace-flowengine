using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class ChildSagaFlowResponseData<TFlowData> where TFlowData : class, new()
    {
        public Guid CorrelationId { get; set; }
        public TFlowData FlowData { get; set; }
    }
}
