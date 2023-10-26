using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.MassTransit.Settings.Configuration;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas
{
    public static class FlowSagaUtil
    {
        internal static FlowTaskDelegate<TFlowData, TSagaContext> CreateChildSagaDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                    IFlowSagaContextRepository repository,
                    RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
                    ChildFlowSaga<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> childFlowSaga,
                    FlowSagaLogger<TFlowData, TSagaContext> logger)
                    where TFlowData : class, new()
                    where TSagaContext : class
                    where TChildFlowData : class, new()
                    where TChildSagaContext : class
        {
            return async (ctx, t) =>
            {
                var flowInstance = ctx.Instance;

                var startDataCreator = new ChildSagaRequestSpaceCreator<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(flowInstance, repository);

                if (childFlowSaga.StartCondition != null)
                {
                    var shouldSend = await childFlowSaga.StartCondition(startDataCreator.CurrentSagaSpace);
                    if (!shouldSend)
                    {
                        await logger.LogFlowRequest(FlowRequestEventType.RequestNotSent, t, flowInstance);
                        t.StatesContainer.SetAll(FlowTaskState.Completed);
                        await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, flowInstance);
                        return;
                    }
                }

                var childSagaChunks = await childFlowSaga.RequestsFactory(startDataCreator);
                flowInstance.FlowData = startDataCreator.CurrentSagaSpace.Data;

                if (!childSagaChunks.Any())
                {
                    await logger.LogFlowRequest(FlowRequestEventType.NoRequests, t, flowInstance);
                    t.StatesContainer.SetAll(FlowTaskState.Completed);
                    await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, flowInstance);
                    return;
                }

                var index = 0;
                var sendTasks = new List<Task>();
                t.StatesContainer = new FlowTaskStateContainer() { ChunkStates = new List<FlowTaskChunkState>() };
                foreach (var childSagaData in childSagaChunks)
                {
                    var chunkIndex = index++;
                    var command = new FlowSagaStartCommand<TChildFlowData>(flowInstance.UserId, flowInstance.UserEmail)
                    {
                        CorrelationId = childSagaData.ChildSagaSpace.CorrelationId,
                        ParentSagaCorrelationId = childSagaData.CurrentSagaSpace.CorrelationId,
                        FlowTaskId = t.Id,
                        FlowTaskChunkIndex = chunkIndex,
                        FlowData = childSagaData.Data,
                    };

                    var sendTask = SendStartChildSagaCommandAsync(ctx, rabbitMqConnectionStringProvider, command);
                    await logger.LogFlowRequest(FlowRequestEventType.RequestSent, t, flowInstance);
                    sendTasks.Add(sendTask);

                    var chunkState = new FlowTaskChunkState()
                    {
                        Index = command.FlowTaskChunkIndex.Value,
                        State = FlowTaskState.Started
                    };
                    t.StatesContainer.ChunkStates.Add(chunkState);
                }

                await Task.WhenAll(sendTasks);
            };
        }

        internal static FlowTaskCompleteChunkDelegate<TFlowData, TSagaContext> CreateChildSagaCompleteChunkDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(
                    IFlowSagaContextRepository repository,
                    RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
                    ChildSagaResponseDelegate<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> responseProcessor)
                    where TFlowData : class, new()
                    where TSagaContext : class
                    where TChildFlowData : class, new()
                    where TChildSagaContext : class
        {
            return async (ctx, flowResponse, t, chunkIndex) =>
            {
                var flowInstance = ctx.Instance;
                var parentSpace = FinallySpace<TFlowData, TSagaContext>.Create(flowInstance, repository);

                var childSpace = FinallySpace<TChildFlowData, TChildSagaContext>.Create(repository);

                if (!string.IsNullOrEmpty(flowResponse?.JsonResult))
                {
                    var childSagaResponseData = JsonConvert.DeserializeObject<ChildSagaFlowResponseData<TChildFlowData>>(flowResponse.JsonResult);
                    childSpace.CorrelationId = childSagaResponseData.CorrelationId;
                    childSpace.Data = childSagaResponseData.FlowData;
                }

                childSpace.UserId = flowInstance.UserId;
                childSpace.ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId;
                childSpace.MainFlowExceptions = flowInstance.FlowContainer
                    .GetMainFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList();
                childSpace.FinallyFlowExceptions = flowInstance.FlowContainer
                    .GetFinallyFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList();

                var space = new ChildSagaResponseSpace<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext>(repository, parentSpace, childSpace);

                if (responseProcessor != null)
                    await responseProcessor.Invoke(space, chunkIndex);

                flowInstance.FlowData = parentSpace.Data;

                var queueAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TChildFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
                var endpoint = await ctx.GetSendEndpoint(queueAddress);
                var command = new FlowSagaEndCommand()
                {
                    CorrelationId = childSpace.CorrelationId,
                };

                var sagaAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
                await endpoint.Send(command, c =>
                {
                    c.FaultAddress = sagaAddress;
                });
            };
        }

        internal static FlowTaskDelegate<TFlowData, TSagaContext> CreateConsumerDelegate<TFlowData, TSagaContext, TRequest>(
            IFlowSagaContextRepository repository,
            RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
            FlowMonolog<TFlowData, TSagaContext, TRequest> flowMonolog,
            FlowSagaLogger<TFlowData, TSagaContext> logger)
            where TRequest : class
            where TFlowData : class, new()
            where TSagaContext : class
        {
            return async (ctx, t) =>
            {
                var flowInstance = ctx.Instance;
                var space = FinallySpace<TFlowData, TSagaContext>.Create(flowInstance, repository);

                if (flowMonolog.SendCondition != null)
                {
                    var shouldSend = await flowMonolog.SendCondition(space);
                    if (!shouldSend)
                    {
                        await logger.LogFlowRequest(FlowRequestEventType.RequestNotSent, t, flowInstance);
                        t.StatesContainer.SetAll(FlowTaskState.Completed);
                        await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, flowInstance);
                        return;
                    }
                }

                var requestChunks = await flowMonolog.RequestFactories(space);
                flowInstance.FlowData = space.Data;

                if (!requestChunks.Any())
                {
                    await logger.LogFlowRequest(FlowRequestEventType.NoRequests, t, flowInstance);
                    t.StatesContainer.SetAll(FlowTaskState.Completed);
                    await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, flowInstance);
                    return;
                }

                var shouldWait = true;
                if (flowMonolog.WaitCondition != null)
                {
                    shouldWait = await flowMonolog.WaitCondition(space);
                }

                var index = 0;
                var sendTasks = new List<Task>();
                t.StatesContainer = new FlowTaskStateContainer() { ChunkStates = new List<FlowTaskChunkState>() };
                foreach (var request in requestChunks)
                {
                    request.CorrelationId = space.CorrelationId;
                    request.FlowTaskId = t.Id;
                    request.FlowTaskChunkIndex = index++;
                    request.DoNotSendResponse = !shouldWait;

                    var sendTask = SendRequestAsync(ctx, rabbitMqConnectionStringProvider, request);
                    var eventType = shouldWait ? FlowRequestEventType.RequestSent : FlowRequestEventType.RequestSentWithNoWait;
                    await logger.LogFlowRequest(eventType, t, flowInstance, request);
                    sendTasks.Add(sendTask);

                    var chunkState = new FlowTaskChunkState()
                    {
                        Index = request.FlowTaskChunkIndex,
                        State = FlowTaskState.Started
                    };
                    t.StatesContainer.ChunkStates.Add(chunkState);
                }

                await Task.WhenAll(sendTasks);

                if (!shouldWait)
                {
                    await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, flowInstance);
                    t.StatesContainer.SetAll(FlowTaskState.Completed);
                }
            };
        }

        internal static FlowTaskCompleteDelegate<TFlowData, TSagaContext> CreateCompleteDelegate<TFlowData, TSagaContext>(
                    IFlowSagaContextRepository repository,
                    CompleteDelegate<TFlowData, TSagaContext> completeDelegate)
                    where TFlowData : class, new()
                    where TSagaContext : class
        {
            if (completeDelegate == null) return null;

            return async (ctx, t) =>
            {
                var space = FinallyFlowSpace<TFlowData, TSagaContext, object>.Create(ctx.Instance, repository, flowResponse: null);

                await completeDelegate(space);
                ctx.Instance.FlowData = space.Data;
            };
        }

        internal static FlowTaskCompleteChunkDelegate<TFlowData, TSagaContext> CreateConsumerCompleteChunkDelegate<TFlowData, TSagaContext, TResponse>(
                    IFlowSagaContextRepository repository,
                    ChunkResponseDelegate<TFlowData, TSagaContext, TResponse> chunkResponseDelegate)
                    where TFlowData : class, new()
                    where TResponse : class
                    where TSagaContext : class
        {
            if (chunkResponseDelegate == null) return null;

            return async (ctx, flowResponse, t, chunkIndex) =>
            {
                var space = FinallyFlowSpace<TFlowData, TSagaContext, TResponse>.Create(ctx.Instance, repository, flowResponse);

                await chunkResponseDelegate(space, chunkIndex);
                ctx.Instance.FlowData = space.Data;
            };
        }

        public static Exception GetExceptionFromFault(ConsumeContext<Fault> ctx)
        {
            var exceptionInfo = ctx.Message.Exceptions[0];
            var exception = GetSelfAndInnerExceptions(exceptionInfo);
            return exception;
        }

        public static Exception GetExceptionFromFault<TFlowData, TSagaContext>(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, Fault> ctx)
            where TFlowData : class, new()
            where TSagaContext : class
        {
            var exceptionInfo = ctx.Data.Exceptions[0];
            var exception = GetSelfAndInnerExceptions(exceptionInfo);
            return exception;
        }

        private static Exception GetSelfAndInnerExceptions(ExceptionInfo exceptionInfo)
        {
            Exception innerException = null;
            if (exceptionInfo.InnerException != null)
            {
                innerException = GetSelfAndInnerExceptions(exceptionInfo.InnerException);
            }

            var messages = new List<string>() {
                            exceptionInfo.Message,
                            exceptionInfo.Source,
                            exceptionInfo.StackTrace,
                        };

            messages = messages
                .Where(x => !string.IsNullOrEmpty(x))
                .ToList();

            var message = string.Join(", ", messages);

            var exception = new Exception(message, innerException);

            return exception;
        }

        private static async Task SendRequestAsync<TFlowData, TRequest, TSagaContext>(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
            FlowRequest<TRequest> request)
            where TRequest : class
            where TFlowData : class, new()
            where TSagaContext : class
        {
            var sagaAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
            var requestType = typeof(TRequest);
            var requestAddress = FlowQueueUtil.GetRequestAddress(requestType, rabbitMqConnectionStringProvider.ConnectionString);
            var endpoint = await ctx.GetSendEndpoint(requestAddress);

            await endpoint.Send(request, c =>
            {
                c.FaultAddress = sagaAddress;
            });
        }

        private static async Task SendStartChildSagaCommandAsync<TFlowData, TChildFlowData, TSagaContext>(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
            FlowSagaStartCommand<TChildFlowData> command)
            where TChildFlowData : class, new()
            where TFlowData : class, new()
            where TSagaContext : class
        {

            var queueAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TChildFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
            var endpoint = await ctx.GetSendEndpoint(queueAddress);

            var sagaAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabbitMqConnectionStringProvider.ConnectionString);
            await endpoint.Send(command, c =>
            {
                c.FaultAddress = sagaAddress;
            });
        }
    }
}
