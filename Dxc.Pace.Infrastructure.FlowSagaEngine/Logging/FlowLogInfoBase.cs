using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public abstract class FlowLogInfoBase
    {
        public Guid CorrelationId { get; set; }
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; }
        public string SagaClassFullName { get; set; }
        public Guid? ParentSagaCorrelationId { get; set; }
    }
}
