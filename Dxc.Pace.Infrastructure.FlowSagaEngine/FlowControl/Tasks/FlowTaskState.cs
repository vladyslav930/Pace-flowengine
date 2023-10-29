namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public enum FlowTaskState
    {
        Initial,
        Started,
        Completed,
        Cancelled,
        Failed,
    }
}
