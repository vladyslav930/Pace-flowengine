using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class FlowSpaceWithNoData<TSagaContext> where TSagaContext : class
    {
        internal Lazy<TSagaContext> SagaContext;

        public TSagaContext Context => SagaContext.Value;
        public Guid CorrelationId { get; internal set; }
        public Guid? UserId { get; internal set; }
        public string UserEmail { get; internal set; }
        public Guid? ParentSagaCorrelationId { get; internal set; }

        internal FlowSpaceWithNoData() { }
    }
}
