namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers
{
    public class FlowConsumerErrorLogInfo<TRequest> : FlowConsumerLogInfo<TRequest> where TRequest : class
    {
        public ExceptionInfo Exception { get; set; }
    }
}
