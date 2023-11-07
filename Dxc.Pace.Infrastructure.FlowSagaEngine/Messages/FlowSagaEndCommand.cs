using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class FlowSagaEndCommand : IFlowMessage
    {
        public Guid CorrelationId { get; set; }
        public int? FlowTaskId { get; set; }
        public int? FlowTaskChunkIndex { get; set; }
    }

}
