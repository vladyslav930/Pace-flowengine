using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public interface IFlowSagaStartCommand : IFlowMessage
    {
        Guid? ParentSagaCorrelationId { get; set; }
        Guid? UserId { get; set; }
        string UserEmail { get; set; }
        string FlowDataTypeFullName { get; }
    }
}
