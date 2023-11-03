namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests
{
    public class FlowRequestLogInfo<TData> : FlowLogInfoBase where TData : class
    {
        public FlowRequestEventType EventType { get; set; }
        public string CallerMethodName { get; set; }
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public TData Request { get; set; }
    }

}
