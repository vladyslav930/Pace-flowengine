using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.MassTransit.Settings.Configuration;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl
{
    internal class FlowProcessor<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        private readonly RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider;
        private readonly ExceptionProcessor<TFlowData, TSagaContext> exceptionProcessor;
        private readonly IFlowSagaContextRepository repository;
        private readonly FlowContainer<TFlowData, TSagaContext> flowContainer;
        private readonly FlowSagaLogger<TFlowData, TSagaContext> logger;

        public FlowProcessor(
            RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
            ExceptionProcessor<TFlowData, TSagaContext> exceptionProcessor,
            IFlowSagaContextRepository repository,
            FlowContainer<TFlowData, TSagaContext> flowContainer,
            FlowSagaLogger<TFlowData, TSagaContext> logger)
        {
            this.rabbitMqConnectionStringProvider = rabbitMqConnectionStringProvider;
            this.exceptionProcessor = exceptionProcessor;
            this.repository = repository;
            this.flowContainer = flowContainer;
            this.logger = logger;
        }

        public async Task CompleteAndProcessTasksAsync(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            int flowTaskId,
            int flowTaskChunkIndex,
            FlowResponse flowResponse)
        {
            var flowTask = flowContainer.ActiveFlow.AllFlowTasks.Single(x => x.Id == flowTaskId);
            await logger.LogFlowRequest(FlowRequestEventType.ResponseReceived, flowTask, ctx.Instance);

            await this.exceptionProcessor.TryDoOrFinallyAsync(
                flowTask,
                ctx,
                () => flowTask.CompleteTaskAsync(ctx, flowTaskChunkIndex, flowResponse),
                flowTaskChunkIndex);

            ctx.Instance.UpdateTasksStates();

            await ProcessTasksAsync(ctx);
        }

        public Task<bool> ProcessExceptionAsync(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, IFlowMessage> ctx,
            Exception exception)
        {
            var flowTaskId = ctx.Data.FlowTaskId.Value;
            var flowTaskChunkIndex = ctx.Data.FlowTaskChunkIndex.Value;

            return ProcessExceptionAsync(flowTaskId, flowTaskChunkIndex, ctx, exception);
        }

        public Task<bool> ProcessExceptionAsync(
            int flowTaskId,
            int flowTaskChunkIndex,
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            Exception exception)
        {
            var flowTask = flowContainer.ActiveFlow.AllFlowTasks.Single(x => x.Id == flowTaskId);
            return this.exceptionProcessor.ProcessExceptionAsync(flowTask, flowTaskChunkIndex, ctx, exception);
        }

        public async Task ProcessTasksAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            if (!flowContainer.CanStartTasks) return;

            var flowTasksToStart = flowContainer.ActiveFlow.GetTasksToStart();
            while (flowContainer.CanStartTasks && flowTasksToStart.Any())
            {
                var tasks = flowTasksToStart
                    .Select(x => TryStartTaskAsync(x, ctx))
                    .ToList();

                await Task.WhenAll(tasks);

                ctx.Instance.UpdateTasksStates();

                flowTasksToStart = flowContainer.ActiveFlow.GetTasksToStart();
            }

            await CheckAndSendSuccessToParentSagaAsync(ctx);
        }

        public async Task CheckAndSendFaultToParentSagaAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            var flowInstance = ctx.Instance;

            if (flowInstance.IsSelfDependent)
                return;

            var exceptions = flowContainer
                .GetAllExceptions()
                .Select(x => x.Exception)
                .ToList();
            var errorMessage = JsonConvert.SerializeObject(exceptions);

            var response = new FaultChildSagaCommand
            {
                CorrelationId = flowInstance.ParentSagaCorrelationId.Value,
                ExceptionMessage = errorMessage,
                FlowTaskId = flowInstance.ParentSagaFlowTaskId,
                FlowTaskChunkIndex = flowInstance.ParentSagaChunkIndex.Value,
            };

            var endpoint = await ctx.GetSendEndpoint(new Uri(flowInstance.ParentSagaAddress));

            await logger.LogFlowSaga(FlowSagaEventType.ParentSagaFaultRequested, flowInstance);

            var sagaAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
            await endpoint.Send(response, c =>
            {
                c.FaultAddress = sagaAddress;
            });

            flowInstance.IsParentSagaRequested = true;
        }

        private async Task CheckAndSendSuccessToParentSagaAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            var flowInstance = ctx.Instance;

            if (!flowContainer.ShouldRequestParentSaga(flowInstance))
                return;

            var responseData = new ChildSagaFlowResponseData<TFlowData>
            {
                CorrelationId = flowInstance.CorrelationId,
                FlowData = flowInstance.FlowData
            };

            var response = new FlowResponse
            {
                CorrelationId = flowInstance.ParentSagaCorrelationId.Value,
                FlowTaskId = flowInstance.ParentSagaFlowTaskId.Value,
                FlowTaskChunkIndex = flowInstance.ParentSagaChunkIndex.Value,
                JsonResult = JsonConvert.SerializeObject(responseData)
            };

            var endpoint = await ctx.GetSendEndpoint(new Uri(flowInstance.ParentSagaAddress));

            await logger.LogFlowSaga(FlowSagaEventType.ParentSagaSuccessRequested, flowInstance);

            var sagaAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
            await endpoint.Send(response, c =>
            {
                c.FaultAddress = sagaAddress;
            });

            flowInstance.IsParentSagaRequested = true;
        }

        private Task TryStartTaskAsync(
            FlowTask<TFlowData, TSagaContext> flowTask,
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            var task = this.exceptionProcessor.TryDoOrFinallyAsync(
                flowTask,
                ctx,
                () => flowTask.StartTaskAsync(ctx, repository),
                chunkIndex: null);

            return task;
        }
    }
}
