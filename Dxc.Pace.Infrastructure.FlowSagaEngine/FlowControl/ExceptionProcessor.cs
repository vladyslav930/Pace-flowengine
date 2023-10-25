using Automatonymous;
using Automatonymous.Lifts;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Settings;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl
{
    internal class ExceptionProcessor<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {

        private readonly FlowSagaLogger<TFlowData, TSagaContext> logger;
        private readonly Func<FinallySpace<TFlowData, TSagaContext>, Task> finallyAsync;
        private readonly IFlowSagaContextRepository repository;
        private readonly FlowEngineSettings flowSagaEngineSettings;

        public ExceptionProcessor(
            IFlowSagaContextRepository repository,
            FlowSagaLogger<TFlowData, TSagaContext> logger,
            Func<FinallySpace<TFlowData, TSagaContext>, Task> finallyAsync,
            FlowEngineSettings flowSagaEngineSettings)
        {
            this.repository = repository;
            this.logger = logger;
            this.finallyAsync = finallyAsync;
            this.flowSagaEngineSettings = flowSagaEngineSettings;
        }

        public async Task TryDoOrFinallyAsync(
            FlowTask<TFlowData, TSagaContext> flowTask,
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            Func<Task> action,
            int? chunkIndex)
        {
            var flowInstance = ctx.Instance;
            var shouldForceFinalize = false;
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                if (!chunkIndex.HasValue)
                {
                    flowTask.StatesContainer.TaskException = new FlowTaskStateException { Exception = exception };
                    flowTask.StatesContainer.SetAll(FlowTaskState.Failed);
                }
                else
                {
                    var state = flowTask.StatesContainer[chunkIndex.Value];
                    state.ChunkException = new FlowTaskStateException { Exception = exception };
                    state.State = FlowTaskState.Failed;
                }

                shouldForceFinalize = !flowInstance.FlowContainer.ActiveFlow.HasPendingTasks;
            }
            finally
            {
                flowInstance.UpdateTasksStates();

                if (shouldForceFinalize || flowInstance.FlowContainer.ShouldFinalize())
                {
                    await CheckProceedToFinallyAsync(ctx);
                }
            }
        }

        public async Task<bool> ProcessExceptionAsync(
            FlowTask<TFlowData, TSagaContext> flowTask,
            int flowTaskChunkIndex,
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            Exception exception)
        {
            var state = flowTask.StatesContainer[flowTaskChunkIndex];
            state.ChunkException = new FlowTaskStateException { Exception = exception };
            if (state.State != FlowTaskState.Started)
            {
                var flowInstance = ctx.Instance;
                await logger.LogFlowSagaError(FlowSagaEventType.DuplicateConsumerCall, flowInstance, new[] { exception });
                return false; 
            }            

            state.State = FlowTaskState.Failed;
            ctx.Instance.UpdateTasksStates();
            await CheckProceedToFinallyAsync(ctx);
            return true;
        }

        public async Task CheckProceedToFinallyAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            var flowInstance = ctx.Instance;
            var flowContainer = flowInstance.FlowContainer;
            var flowProcessor = flowInstance.FlowProcessor;

            if (flowContainer.IsMainSuccessAndFinallySuccess())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainSuccessAndFinallySuccess", flowInstance);
                await TryFinallyAsync(flowInstance);
                return;
            }

            if (flowContainer.IsMainSuccessAndFinallyFailed())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainSuccessAndFinallyFailed", flowInstance);
                await logger.LogFlowSagaError(flowContainer, flowInstance);
                await TryFinallyAsync(flowInstance);
                await flowProcessor.CheckAndSendFaultToParentSagaAsync(ctx);
                return;
            }

            if (flowContainer.IsMainFailedAndFinallyIsInInitialState())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainFailedAndFinallyNotStarted", flowInstance);
                await logger.LogFlowSagaError(flowContainer, flowInstance);
                await flowProcessor.ProcessTasksAsync(ctx);
                return;
            }

            if (flowContainer.IsMainFailedAndFinallyIsStarted())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainFailedAndFinallyIsStarted", flowInstance);
                return;
            }

            if (flowContainer.IsMainFailedAndFinallySuccess())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainFailedAndFinallySuccess", flowInstance);
                await TryFinallyAsync(flowInstance);
                await flowProcessor.CheckAndSendFaultToParentSagaAsync(ctx);
                return;
            }

            if (flowContainer.IsMainFailedAndFinallyFailed())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync IsMainFailedAndFinallyFailed", flowInstance);
                await logger.LogFlowSagaError(flowContainer, flowInstance);
                await TryFinallyAsync(flowInstance);
                await flowProcessor.CheckAndSendFaultToParentSagaAsync(ctx);
                return;
            }

            if (flowContainer.ActiveFlow.GetTasksToStart().ToList().Any())
            {
                await LogExtendedFlowEngineMessage($"CheckProceedToFinallyAsync ShouldIngonreMainFailed", flowInstance);
                await flowProcessor.ProcessTasksAsync(ctx);
                return;
            }

            var infoStr = flowContainer.Serialize();
            var message = $"CheckProceedToFinallyAsync Unreachable {infoStr}";
            await LogExtendedFlowEngineMessage(message, flowInstance);
            var exception = new Exception("Unreachable");
            await logger.LogFlowSagaError(flowContainer, flowInstance, exception);

            throw exception;
        }

        private async Task TryFinallyAsync(FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            try
            {
                var space = FinallySpace<TFlowData, TSagaContext>.Create(flowInstance, repository);
                await this.finallyAsync(space);
            }
            catch (Exception finallyException)
            {
                await logger.LogFlowSagaError(flowInstance.FlowContainer, flowInstance, finallyException);
                await LogExtendedFlowEngineMessage($"TryFinallyAsync catch", flowInstance);
            }
            finally
            {
                await logger.LogFlowSaga(FlowSagaEventType.SagaCompleted, flowInstance);
            }
        }

        private async Task LogExtendedFlowEngineMessage(string message, FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            var isAllowed = flowSagaEngineSettings.LogLevel == FlowLogLevel.Extended;
            if (!isAllowed) return;

            await logger.LogFlowSaga(FlowSagaEventType.SagaDoFinallyExtendedEvent, flowInstance, message);
        }
    }
}
