using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl
{
    internal delegate Task FlowTaskDelegate<TFlowData, TSagaContext>(
        BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx, FlowTask<TFlowData, TSagaContext> flowTask)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task FlowTaskCompleteChunkDelegate<TFlowData, TSagaContext>(
        BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
        FlowResponse flowResponse,
        FlowTask<TFlowData, TSagaContext> flowTask,
        int chunkIndex)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task FlowTaskCompleteDelegate<TFlowData, TSagaContext>(
        BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx, FlowTask<TFlowData, TSagaContext> flowTask)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task<bool> FlowTaskCheckConditionDelegate<TFlowData, TSagaContext>(FlowSpace<TFlowData, TSagaContext> space)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task DoFuncDelegate<TFlowData, TSagaContext>(FlowSpace<TFlowData, TSagaContext> space)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task DoUnsafeFuncDelegate<TFlowData, TSagaContext>(UnsafeFlowSpace<TFlowData, TSagaContext> space)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task<List<FlowRequest<TRequest>>> RequestsFactoryDelegate<TFlowData, TSagaContext, TRequest>(
        FlowSpace<TFlowData, TSagaContext> space)
        where TRequest : class
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task CompleteDelegate<TFlowData, TSagaContext>(FlowSpace<TFlowData, TSagaContext> space)
        where TFlowData : class, new()
        where TSagaContext : class;

    internal delegate Task ChunkResponseDelegate<TFlowData, TSagaContext, TResponse>(FlowSpace<TFlowData, TSagaContext, TResponse> space, int chunkIndex)
        where TFlowData : class, new()
        where TResponse : class
        where TSagaContext : class;

    internal delegate Task<List<ChildSagaRequestSpaceWithData<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>>> ChildSagaRequestFactoryDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
        ChildSagaRequestSpaceCreator<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> startDataFactory)
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class;

    internal delegate Task ChildSagaResponseDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
        ChildSagaResponseSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> space, int chunkIndex)
        where TFlowData : class, new()
        where TSagaContext : class
        where TChildFlowData : class, new()
        where TChildSagaContext : class;
}
