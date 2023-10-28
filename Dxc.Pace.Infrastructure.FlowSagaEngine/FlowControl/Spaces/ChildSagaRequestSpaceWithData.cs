using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
            : ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
            where TFlowData : class, new()
            where TSagaContext : class
            where TChildFlowData : class, new()
            where TChildSagaContext : class
    {
        internal readonly TChildFlowData Data;

        internal ChildSagaRequestSpaceWithData(
            IFlowSagaContextRepository repository,
            FlowSpace<TFlowData, TSagaContext> parentSagaSpace,
            FlowSpace<TChildFlowData, TChildSagaContext> childSagaSpace,
            TChildFlowData data) : base(repository, parentSagaSpace, childSagaSpace)

        {
            this.Data = data;
        }
    }
}
