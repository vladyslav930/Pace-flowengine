using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class ChildSagaRequestSpaceCreator<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class
    {

        public readonly FinallySpace<TFlowData, TSagaContext> CurrentSagaSpace;
        private readonly FlowInstance<TFlowData, TSagaContext> flowInstance;
        private readonly IFlowSagaContextRepository repository;
        private readonly List<Exception> mainFlowExceptions;
        private readonly List<Exception> finallyFlowExceptions;
        private readonly string mainFlowTaskStatesJson;
        private readonly string finallyFlowTaskStatesJson;

        internal ChildSagaRequestSpaceCreator(
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            IFlowSagaContextRepository repository)
        {
            this.CurrentSagaSpace = FinallySpace<TFlowData, TSagaContext>.Create(flowInstance, repository);
            this.flowInstance = flowInstance;
            this.repository = repository;
            this.mainFlowExceptions = flowInstance.FlowContainer
                .GetMainFlowExceptions()
                .Select(x => x.Exception)
                .ToList();
            this.finallyFlowExceptions = flowInstance.FlowContainer
                .GetFinallyFlowExceptions()
                .Select(x => x.Exception)
                .ToList();
            this.mainFlowTaskStatesJson = flowInstance.MainFlowTaskStatesJson;
            this.finallyFlowTaskStatesJson = flowInstance.FinallyFlowTaskStatesJson;
        }

        public ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> Create(
            TChildFlowData data)
        {
            var childSpace = CreateChildSpace();

            var startSpaceWithData = new ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                this.repository, this.CurrentSagaSpace, childSpace, data);

            return startSpaceWithData;
        }

        public async Task<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>> Create(
            Func<ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, Task<TChildFlowData>> dataFactory)
        {
            var childSpace = CreateChildSpace();
            var startSpace = new ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                this.repository, this.CurrentSagaSpace, childSpace);
            var data = await dataFactory(startSpace);
            var startChunkSpace = new ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                this.repository, this.CurrentSagaSpace, childSpace, data);

            return startChunkSpace;
        }

        private FinallySpace<TChildFlowData, TChildSagaContext> CreateChildSpace()
        {
            var childSpace = FinallySpace<TChildFlowData, TChildSagaContext>.Create(this.repository);
            childSpace.CorrelationId = NewId.NextGuid();

            childSpace.UserId = flowInstance.UserId;
            childSpace.UserEmail = flowInstance.UserEmail;
            childSpace.ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId;
            childSpace.MainFlowExceptions = this.mainFlowExceptions;
            childSpace.FinallyFlowExceptions = this.finallyFlowExceptions;
            childSpace.MainFlowTaskStatesJson = this.mainFlowTaskStatesJson;
            childSpace.FinallyFlowTaskStatesJson = this.finallyFlowTaskStatesJson;

            return childSpace;
        }
    }
}
