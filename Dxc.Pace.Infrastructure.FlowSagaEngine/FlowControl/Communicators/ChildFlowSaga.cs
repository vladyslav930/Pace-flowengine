using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators
{
    public class ChildFlowSaga<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class
    {
        internal ChildFlowSaga() { }

        internal FlowTaskCheckConditionDelegate<TFlowData, TSagaContext> StartCondition { get; set; }
        internal ChildSagaRequestFactoryDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> RequestsFactory { get; set; }
        internal ChildSagaResponseDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> ChunkResponseProcessor { get; set; }
        internal CompleteDelegate<TFlowData, TSagaContext> CompleteProcessor { get; set; }
        public string Name { get; set; }
        internal string CallerMethodName { get; set; }

        public void SetStartCondition(Func<FlowSpace<TFlowData, TSagaContext>, bool> func)
        {
            StartCondition = s => { return Task.FromResult(func(s)); };
        }

        public void SetStartCondition(Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> func)
        {
            StartCondition = async s => { return await func(s); };
        }

        public void SetRequestFactory(
            Func<ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, Task<TChildFlowData>> dataFactory)
        {
            RequestsFactory = async c =>
            {
                var startSpace = await c.Create(dataFactory);
                return new List<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>>() { startSpace };
            };
        }

        public void SetRequestFactory(
                    Func<ChildSagaRequestSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, TChildFlowData> dataFactory)
        {
            RequestsFactory = async c =>
            {
                var startSpace = await c.Create(s => Task.FromResult(dataFactory(s)));
                return new List<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>>() { startSpace };
            };
        }

        public void SetRequestsFactory(
            Func<ChildSagaRequestSpaceCreator<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, Task<IEnumerable<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>>>> dataFactory)
        {
            RequestsFactory = async c =>
            {
                var spaces = await dataFactory(c);
                return spaces.ToList();
            };
        }

        public void SetRequestsFactory(
            Func<ChildSagaRequestSpaceCreator<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, IEnumerable<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>>> dataFactory)
        {
            RequestsFactory = c =>
            {
                var spaces = dataFactory(c).ToList();
                return Task.FromResult(spaces);
            };
        }

        public void SetChunkResponseProcessor(
            Func<ChildSagaResponseSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, int, Task> func)
        {
            ChunkResponseProcessor = async (s, chunkIndex) => { await func(s, chunkIndex); };
        }

        public void SetChunkResponseProcessor(
            Action<ChildSagaResponseSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>, int> func)
        {
            ChunkResponseProcessor = (s, chunkIndex) =>
            {
                func(s, chunkIndex);
                return Task.CompletedTask;
            };
        }

        public void SetOnCompleteProcessor(
            Func<FlowSpace<TFlowData, TSagaContext>, Task> func)
        {
            CompleteProcessor = async s => { await func(s); };
        }

        public void SetOnCompleteProcessor(
            Action<FlowSpace<TFlowData, TSagaContext>> func)
        {
            CompleteProcessor = s =>
            {
                func(s);
                return Task.CompletedTask;
            };
        }
    }

}
