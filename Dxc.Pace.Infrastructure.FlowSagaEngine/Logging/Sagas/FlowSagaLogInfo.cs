namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas
{
    public class FlowSagaLogInfo : FlowLogInfoBase
    {
        public FlowSagaEventType EventType { get; set; }
        public string Message { get; set; }
        public string FlowDataTypeFullName { get; set; }
    }
}
