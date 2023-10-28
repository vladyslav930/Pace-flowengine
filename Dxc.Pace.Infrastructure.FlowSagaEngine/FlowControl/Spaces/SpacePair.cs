using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    internal class SpacePair<TFlowData1, TSagaContext1, TFlowData2, TSagaContext2>
        where TFlowData1 : class, new()
        where TSagaContext1 : class
        where TFlowData2 : class, new()
        where TSagaContext2 : class
    {
        private readonly IFlowSagaContextRepository repository;
        internal readonly FlowSpace<TFlowData1, TSagaContext1> Space1;
        internal readonly FlowSpace<TFlowData2, TSagaContext2> Space2;

        internal SpacePair(
            IFlowSagaContextRepository repository,
            FlowSpace<TFlowData1, TSagaContext1> space1,
            FlowSpace<TFlowData2, TSagaContext2> space2)
        {
            this.repository = repository;
            Space1 = space1;
            Space2 = space2;
        }

        public async Task MoveSagaRepositoryAsync<TRepositoryData>(
            Func<TSagaContext2, IFlowSagaContextRepository<TRepositoryData>> sourceRepositorySelector,
            Func<TSagaContext1, IFlowSagaContextRepository<TRepositoryData>> targetRepositorySelector)
        {
            var childRepositoryKey = sourceRepositorySelector(Space2.Context).GetKey();
            var parentRepositoryKey = targetRepositorySelector(Space1.Context).GetKey();
            await repository.RenameAsync(childRepositoryKey, parentRepositoryKey);
        }

        public async Task MoveSagaValueAsync<TRepositoryData>(
            Func<TSagaContext2, IFlowSagaContextValue<TRepositoryData>> sourceValueSelector,
            Func<TSagaContext1, IFlowSagaContextValue<TRepositoryData>> targetValueSelector)
        {
            var childRepositoryKey = sourceValueSelector(Space2.Context).GetKey();
            var parentRepositoryKey = targetValueSelector(Space1.Context).GetKey();
            await repository.RenameAsync(childRepositoryKey, parentRepositoryKey);
        }
    }
}
