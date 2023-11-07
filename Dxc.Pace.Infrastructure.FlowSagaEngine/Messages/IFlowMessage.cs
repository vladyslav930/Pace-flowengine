using MassTransit;
using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public interface IFlowMessage : CorrelatedBy<Guid>
    {
        int? FlowTaskId { get; set; }
        int? FlowTaskChunkIndex { get; set; }
    }
}
