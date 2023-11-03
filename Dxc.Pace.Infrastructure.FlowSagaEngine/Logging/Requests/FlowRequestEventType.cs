namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests
{
    public enum FlowRequestEventType
    {
        RequestSent = 0,
        RequestSentWithNoWait = 1,
        RequestNotSent = 2,
        NoRequests = 3,
        ResponseReceived = 4,
    }
}
