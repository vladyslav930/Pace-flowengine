namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks
{
    public class FlowTaskLogInfo<TFlowData> : FlowLogInfoBase
        where TFlowData : class, new()
    {
        public FlowTaskEventType EventType { get; set; }
        public string CallerMethodName { get; set; }
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public TFlowData FlowData { get; set; }
    }
}
