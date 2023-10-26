using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class
    {
        private readonly SpacePair<TChildFlowData, TChildSagaContext, TFlowData, TSagaContext> spacePair;
        
        public FlowSpace<TFlowData, TSagaContext> CurrentSagaSpace => spacePair.Space2;
        public FlowSpaceWithNoData<TChildSagaContext> ChildSagaSpace => spacePair.Space1;

        internal ChildSagaRequestSpace(
            IFlowSagaContextRepository repository,
            FlowSpace<TFlowData, TSagaContext> parentSagaSpace,
            FlowSpace<TChildFlowData, TChildSagaContext> childSagaSpace)
        {
            spacePair = new SpacePair<TChildFlowData, TChildSagaContext, TFlowData, TSagaContext>(
                repository, childSagaSpace, parentSagaSpace);
        }

        public Task MoveSagaRepositoryAsync<TRepositoryData>(
            Func<TSagaContext, IFlowSagaContextRepository<TRepositoryData>> sourceRepositorySelector,
            Func<TChildSagaContext, IFlowSagaContextRepository<TRepositoryData>> targetRepositorySelector)
        {
            return spacePair.MoveSagaRepositoryAsync(sourceRepositorySelector, targetRepositorySelector);
        }

        public Task MoveSagaValueAsync<TRepositoryData>(
            Func<TSagaContext, IFlowSagaContextValue<TRepositoryData>> sourceValueSelector,
            Func<TChildSagaContext, IFlowSagaContextValue<TRepositoryData>> targetValueSelector)
        {
            return spacePair.MoveSagaValueAsync(sourceValueSelector, targetValueSelector);
        }
    }
}
