using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class ChildSagaResponseSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class
    {
        private readonly SpacePair<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> spacePair;

        public FlowSpace<TFlowData, TSagaContext> CurrentSagaSpace => spacePair.Space1;
        public FlowSpace<TChildFlowData, TChildSagaContext> ChildSagaSpace => spacePair.Space2;

        internal ChildSagaResponseSpace(
            IFlowSagaContextRepository repository,
            FlowSpace<TFlowData, TSagaContext> parentSagaSpace,
            FlowSpace<TChildFlowData, TChildSagaContext> childSagaSpace)
        {
            spacePair = new SpacePair<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                repository, parentSagaSpace, childSagaSpace);
        }

        public Task MoveSagaRepositoryAsync<TRepositoryData>(
            Func<TChildSagaContext, IFlowSagaContextRepository<TRepositoryData>> sourceRepositorySelector,
            Func<TSagaContext, IFlowSagaContextRepository<TRepositoryData>> targetRepositorySelector)
        {
            return spacePair.MoveSagaRepositoryAsync(sourceRepositorySelector, targetRepositorySelector);
        }

        public Task MoveSagaValueAsync<TRepositoryData>(
            Func<TChildSagaContext, IFlowSagaContextValue<TRepositoryData>> sourceValueSelector,
            Func<TSagaContext, IFlowSagaContextValue<TRepositoryData>> targetValueSelector)
        {
            return spacePair.MoveSagaValueAsync(sourceValueSelector, targetValueSelector);
        }
    }
}
