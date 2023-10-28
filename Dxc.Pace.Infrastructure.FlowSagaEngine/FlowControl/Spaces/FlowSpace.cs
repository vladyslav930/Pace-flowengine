namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class FlowSpace<TFlowData, TSagaContext> : FlowSpaceWithNoData<TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public TFlowData Data { get; internal set; }

        internal FlowSpace() { }
    }

    public class FlowSpace<TFlowData, TSagaContext, TResponse> : FlowSpace<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
        where TResponse : class
    {
        public TResponse Response { get; internal set; }
        internal FlowSpace() { }
    }
}
