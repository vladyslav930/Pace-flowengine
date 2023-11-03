namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas
{
    public enum FlowSagaEventType
    {
        SagaStarted = 0,
        SagaCompleted = 1,

        SagaMainFlowError = 2,
        SagaFinallyFlowError = 3,

        ParentSagaSuccessRequested = 4,
        ParentSagaFaultRequested = 5,

        ParentSagaSuccessResponded = 6,
        ParentSagaFaultResponded = 7,

        SagaDoFinallyExtendedEvent = 8,

        SagaStartReceivedDuringStartedState = 9,

        FaultReceived = 10,

        CanStartError = 11,

        DuplicateConsumerCall = 12,

        SagaStartDelayed = 100,
    }
}
