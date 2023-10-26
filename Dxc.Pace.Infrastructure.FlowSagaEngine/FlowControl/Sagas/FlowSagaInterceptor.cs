namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas
{
    public abstract class FlowSagaInterceptor
    {
        public virtual void OnSagaStart() { }
        public virtual void OnLaunchSagaEvent() { }
        public virtual void OnConsumerCompletedEvent() { }
        public virtual void OnConsumerFailedEvent() { }
        public virtual void OnFaildToSendStartChildEvent() { }
        public virtual void OnChildSagaFailedEvent() { }
        public virtual void OnFailedToSendChildSagaFailedEvent() { }
        public virtual void OnFinalizeFromParentSagaEvent() { }
        public virtual void OnFailedToSendSuccessToParentEvent() { }
        public virtual void OnFailedToSendEndSagaToChildEvent() { }
        public virtual void DuringLaunchSagaEventOnTransitionToWaitingParentSaga() { }
        public virtual void DuringConsumerCompletedEventOnTransitionToWaitingParentSaga() { }
    }
}
