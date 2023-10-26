using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Settings;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using Hangfire;
using Dxc.Pace.Infrastructure.Hangfire.Abstractions;
using Dxc.Pace.Infrastructure.MassTransit.Logging.Helpers;
using Dxc.Pace.Infrastructure.MassTransit.Settings.Configuration;
using Dxc.Pace.Infrastructure.Core.Configuration.LogManager;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas
{
    public abstract class FlowSaga<TFlowData, TSagaContext> : MassTransitStateMachine<FlowInstance<TFlowData, TSagaContext>>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        private static readonly TimeSpan canNotStartSagaRetryDelay = TimeSpan.FromSeconds(5);

        private readonly Flow<TFlowData, TSagaContext> masterMainFlow;
        private readonly Flow<TFlowData, TSagaContext> masterFinallyFlow;
        private Flow<TFlowData, TSagaContext> masterActiveFlow;

        private readonly IFlowSagaContextRepository repository;
        private readonly RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider;
        private readonly FlowSagaLogger<TFlowData, TSagaContext> logger;
        private readonly ExceptionProcessor<TFlowData, TSagaContext> exceptionProcessor;
        private readonly FlowEngineSettings flowSagaEngineSettings;

        public FlowCommunicatorFactory<TFlowData, TSagaContext> Create { get; }
            = new FlowCommunicatorFactory<TFlowData, TSagaContext>();

        public static Event<FlowSagaStartCommand<TFlowData>> StartSagaEvent { get; private set; }
        public static Event<FlowSagaLaunchCommand<TFlowData>> LaunchSagaEvent { get; private set; }
        public static Event<FlowResponse> ConsumerCompletedEvent { get; private set; }
        public static Event<FlowSagaEndCommand> FinalizeFromParentSagaEvent { get; private set; }

        public static Event<FailedFlowResponse> ConsumerFailedEvent { get; private set; }
        public static Event<FaultChildSagaCommand> ChildSagaFailedEvent { get; private set; }
        public static Event<Fault<FaultChildSagaCommand>> FailedToSendChildSagaFailedEvent { get; private set; }
        public static Event<Fault<IFlowSagaStartCommand>> FaildToSendStartChildEvent { get; private set; }
        public static Event<Fault<FlowResponse>> FailedToSendSuccessToParentEvent { get; private set; }
        public static Event<Fault<FlowSagaEndCommand>> FailedToSendEndSagaToChildEvent { get; private set; }
        public State Launched { get; private set; }
        public State WaitingParentSaga { get; private set; }

        protected IConfiguration Configuration { get; }
        public IServiceProvider ServiceProvider { get; private set; }

        protected IFlowSagaLogger FlowSagaLogger { get; private set; }
        protected FlowLogLevel FlowSagaLogLevel => flowSagaEngineSettings.LogLevel;

        public FlowSaga(IServiceProvider serviceProvider)
        {
            masterMainFlow = new Flow<TFlowData, TSagaContext>(this.ShouldIngonreMainFailed);
            masterFinallyFlow = new Flow<TFlowData, TSagaContext>(shouldIngonreFailed: false);

            ServiceProvider = serviceProvider;
            Configuration = serviceProvider.GetService<IConfiguration>();
            repository = serviceProvider.GetRequiredService<IFlowSagaContextRepository>();
            rabbitMqConnectionStringProvider = serviceProvider.GetRequiredService<RabbitMqConnectionStringProvider>();
            flowSagaEngineSettings = serviceProvider.GetRequiredService<FlowEngineSettingsStorage>().FlowSagaEngineSettings;

            FlowSagaLogger = serviceProvider.GetRequiredService<IFlowSagaLogger>();

            logger = new FlowSagaLogger<TFlowData, TSagaContext>(
                FlowSagaLogger,
                GetType(),
                flowSagaEngineSettings.LogLevel);

            exceptionProcessor = new ExceptionProcessor<TFlowData, TSagaContext>(repository, logger, this.FinallyAsync, flowSagaEngineSettings);

            ConfigureMainAndFinallyFlows();

            ConfigureMassTransitStateMachine();
        }

        public FlowTaskChain<TFlowData, TSagaContext> Do(Action<FlowSpace<TFlowData, TSagaContext>> func)
        {
            Task doDelegate(FlowSpace<TFlowData, TSagaContext> s)
            {
                func(s);
                return Task.CompletedTask;
            }

            return Do(null, (DoFuncDelegate<TFlowData, TSagaContext>)doDelegate);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Do(string name, Action<FlowSpace<TFlowData, TSagaContext>> func)
        {
            Task doDelegate(FlowSpace<TFlowData, TSagaContext> s)
            {
                func(s);
                return Task.CompletedTask;
            }

            return Do(name, (DoFuncDelegate<TFlowData, TSagaContext>)doDelegate);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Do(Func<FlowSpace<TFlowData, TSagaContext>, Task> func = null)
        {
            var isFlowEngineTask = false;
            if (func == null)
            {
                func = s => Task.CompletedTask;
                isFlowEngineTask = true;
            }
            async Task doDelegate(FlowSpace<TFlowData, TSagaContext> s) { await func(s); }
            return Do(null, doDelegate, isFlowEngineTask);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Do(string name, Func<FlowSpace<TFlowData, TSagaContext>, Task> func = null)
        {
            if (func == null)
            {
                func = s => Task.CompletedTask;
            }
            async Task doDelegate(FlowSpace<TFlowData, TSagaContext> s) { await func(s); }
            return Do(name, (DoFuncDelegate<TFlowData, TSagaContext>)doDelegate);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Do(params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            var newChain = Do();
            return newChain.Then(chains);
        }

        public FlowTaskChain<TFlowData, TSagaContext> DoIf(
            Func<FlowSpace<TFlowData, TSagaContext>, bool> doConditionFunc,
            params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            return Do().ThenIf(doConditionFunc, chains);
        }

        public FlowTaskChain<TFlowData, TSagaContext> DoIf(
            Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> doConditionFunc,
            params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            return Do().ThenIf(doConditionFunc, chains);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Send<TRequest>(
                    FlowMonolog<TFlowData, TSagaContext, TRequest> monolog)
                    where TRequest : class
        {
            var dialog = new FlowDialog<TFlowData, TSagaContext, TRequest, object>(monolog);
            return Send(dialog);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Send<TRequest, TResponse>(
            FlowDialog<TFlowData, TSagaContext, TRequest, TResponse> flowDialog)
            where TRequest : class
            where TResponse : class
        {
            var taskDelegate = FlowSagaUtil.CreateConsumerDelegate(
                repository,
                rabbitMqConnectionStringProvider,
                flowDialog,
                logger);

            var completeDelegate = FlowSagaUtil.CreateCompleteDelegate(repository, flowDialog.CompleteProcessor);
            var completeChunkDelegate = FlowSagaUtil.CreateConsumerCompleteChunkDelegate(repository, flowDialog.ChunkResponseProcessor);

            var task = new FlowTask<TFlowData, TSagaContext>(masterActiveFlow, taskDelegate, completeDelegate, completeChunkDelegate, logger)
            {
                Name = flowDialog.Name,
                CallerMethodName = flowDialog.CallerMethodName,
            };
            return new FlowTaskChain<TFlowData, TSagaContext>(task);
        }

        public FlowTaskChain<TFlowData, TSagaContext> StartChildSaga<TChildFlowData, TChildSagaContext>(
            ChildFlowSaga<TFlowData, TSagaContext, TChildFlowData, TChildSagaContext> childFlowSaga)
            where TChildFlowData : class, new()
            where TChildSagaContext : class
        {
            var taskDelegate = FlowSagaUtil.CreateChildSagaDelegate(
                repository,
                rabbitMqConnectionStringProvider,
                childFlowSaga,
                logger);

            var completeDelegate = FlowSagaUtil.CreateCompleteDelegate(repository, childFlowSaga.CompleteProcessor);
            var completeChunkDelegate = FlowSagaUtil.CreateChildSagaCompleteChunkDelegate(
                repository, rabbitMqConnectionStringProvider, childFlowSaga.ChunkResponseProcessor);

            var task = new FlowTask<TFlowData, TSagaContext>(masterActiveFlow, taskDelegate, completeDelegate, completeChunkDelegate, logger)
            {
                Name = childFlowSaga.Name,
                CallerMethodName = childFlowSaga.CallerMethodName,
            };
            return new FlowTaskChain<TFlowData, TSagaContext>(task);
        }

        protected virtual FlowSagaInterceptor Interceptor => null;

        protected virtual Task<bool> CanStartWithLockAsync(FlowSpace<TFlowData, TSagaContext> space) { return Task.FromResult(true); }

        protected virtual bool ShouldLaunchImmediately => true;
        protected virtual bool ShouldIngonreMainFailed => false;
        protected virtual Task DelayLaunch(
            UnsafeFlowSpaceWithNoContext<TFlowData, TSagaContext> space,
            FlowSagaLaunchCommand<TFlowData> targetLaunchCommand,
            Uri targetQueueAddress,
            Uri targetFaultAddress,
            CorrelationLogManager targetCorrelationInfo)
        {
            const string message = "Implement DelayLaunch in case ShouldLaunchImmediately returns false";
            throw new NotImplementedException(message);
        }

        protected abstract void ConfigureFlow();

        protected virtual void ConfigureFinallyFlow() { }

        protected virtual async Task FinallyAsync(FinallySpace<TFlowData, TSagaContext> space)
        {
            await FlowSagaContextUtil.ClearSagaContextAsync<TSagaContext>(space.CorrelationId, repository);
        }


        public FlowTaskChain<TFlowData, TSagaContext> DoUnsafe(
            string name,
            Func<UnsafeFlowSpace<TFlowData, TSagaContext>, Task> func)
        {
            var callerMethodName = new StackTrace().GetFrame(1).GetMethod().Name;
            var taskDelegate = CreateAsyncTaskDelegate(async s => await func(s));
            var task = new FlowTask<TFlowData, TSagaContext>(masterActiveFlow, taskDelegate, logger)
            {
                Name = name,
                CallerMethodName = callerMethodName
            };
            return new FlowTaskChain<TFlowData, TSagaContext>(task);
        }

        private void ConfigureMassTransitStateMachine()
        {
            InstanceState(x => x.CurrentState);

            Event(() => StartSagaEvent, x =>
            {
                x.CorrelateById(ctx => ctx.Message.CorrelationId);
                x.InsertOnInitial = true;
            });
            Event(() => LaunchSagaEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
            Event(() => ConsumerCompletedEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
            Event(() => FinalizeFromParentSagaEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

            Event(() => ConsumerFailedEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));
            Event(() => ChildSagaFailedEvent, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

            Event(() => FaildToSendStartChildEvent, x => x.CorrelateById(ctx => ctx.Message.Message.CorrelationId));
            Event(() => FailedToSendChildSagaFailedEvent, x => x.CorrelateById(ctx => ctx.Message.Message.CorrelationId));
            Event(() => FailedToSendSuccessToParentEvent, x => x.CorrelateById(ctx => ctx.Message.Message.CorrelationId));
            Event(() => FailedToSendEndSagaToChildEvent, x => x.CorrelateById(ctx => ctx.Message.Message.CorrelationId));

            Initially(
                When(StartSagaEvent)
                    .ThenAsync(ProcessStartSagaEvent)
                    .TransitionTo(Launched)
            );

            During(Launched,
                When(StartSagaEvent)
                    .Then(ctx => this.Interceptor?.OnSagaStart())
                    .ThenAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        await this.logger.LogFlowSaga(FlowSagaEventType.SagaStartReceivedDuringStartedState, ctx.Instance);
                    }),
                When(LaunchSagaEvent)
                    .Then(ctx => this.Interceptor?.OnLaunchSagaEvent())
                    .ThenAsync(ProcessLaunchSagaEvent)
                    .If(ctx => ctx.Instance.IsParentSagaRequested, x =>
                    {
                        this.Interceptor?.DuringLaunchSagaEventOnTransitionToWaitingParentSaga();
                        return x.TransitionTo(WaitingParentSaga);
                    })
                    .If(ctx => ctx.Instance.FlowContainer.ShouldFinalize(), x => x.Finalize()),
                When(ConsumerCompletedEvent)
                    .Then(ctx => this.Interceptor?.OnConsumerCompletedEvent())
                    .ThenAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var flowTaskId = ctx.Data.FlowTaskId.Value;
                        var flowTaskChunkIndex = ctx.Data.FlowTaskChunkIndex.Value;
                        var flowResponse = ctx.Data;
                        await ctx.Instance.FlowProcessor.CompleteAndProcessTasksAsync(ctx, flowTaskId, flowTaskChunkIndex, flowResponse);
                    })
                    .If(ctx => ctx.Instance.IsParentSagaRequested, x =>
                    {
                        this.Interceptor?.DuringConsumerCompletedEventOnTransitionToWaitingParentSaga();
                        return x.TransitionTo(WaitingParentSaga);
                    })
                    .If(ctx => ctx.Instance.FlowContainer.ShouldFinalize(), x => x.Finalize()),

                When(ConsumerFailedEvent)
                    .Then(ctx => this.Interceptor?.OnConsumerFailedEvent())
                    .IfAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var innerException = new Exception(ctx.Data.ExceptionMessage);
                        var exception = new Exception(nameof(ConsumerFailedEvent), innerException);
                        var shouldContinue = await ctx.Instance.FlowProcessor.ProcessExceptionAsync(ctx, exception);
                        return !shouldContinue ? false : ctx.Instance.FlowContainer.ShouldFinalize();
                    }, x => x.Finalize()),
                When(FaildToSendStartChildEvent)
                    .Then(ctx => this.Interceptor?.OnFaildToSendStartChildEvent())
                    .IfAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var innerException = FlowSagaUtil.GetExceptionFromFault(ctx);
                        var exception = new Exception(nameof(FaildToSendStartChildEvent), innerException);
                        var flowTaskId = ctx.Data.Message.FlowTaskId.Value;
                        var flowTaskChunkIndex = ctx.Data.Message.FlowTaskChunkIndex.Value;
                        var shouldContinue = await ctx.Instance.FlowProcessor.ProcessExceptionAsync(flowTaskId, flowTaskChunkIndex, ctx, exception);
                        return !shouldContinue ? false : ctx.Instance.FlowContainer.ShouldFinalize();
                    }, x => x.Finalize()),
                When(ChildSagaFailedEvent)
                    .Then(ctx => this.Interceptor?.OnChildSagaFailedEvent())
                    .IfAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var innerException = new Exception(ctx.Data.ExceptionMessage);
                        var exception = new Exception(nameof(ChildSagaFailedEvent), innerException);
                        var shouldContinue = await ctx.Instance.FlowProcessor.ProcessExceptionAsync(ctx, exception);
                        return !shouldContinue ? false : ctx.Instance.FlowContainer.ShouldFinalize();
                    }, x => x.Finalize()),
                When(FailedToSendChildSagaFailedEvent)
                    .Then(ctx => this.Interceptor?.OnFailedToSendChildSagaFailedEvent())
                    .IfAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var innerException = FlowSagaUtil.GetExceptionFromFault(ctx);
                        var exception = new Exception(nameof(FailedToSendChildSagaFailedEvent), innerException);
                        var flowTaskId = ctx.Data.Message.FlowTaskId.Value;
                        var flowTaskChunkIndex = ctx.Data.Message.FlowTaskChunkIndex.Value;

                        var shouldContinue = await ctx.Instance.FlowProcessor.ProcessExceptionAsync(flowTaskId, flowTaskChunkIndex, ctx, exception);
                        return !shouldContinue ? false : ctx.Instance.FlowContainer.ShouldFinalize();
                    }, x => x.Finalize())
            );

            During(WaitingParentSaga,
                When(FinalizeFromParentSagaEvent)
                    .Then(ctx => this.Interceptor?.OnFinalizeFromParentSagaEvent())
                    .ThenAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        await logger.LogFlowSaga(FlowSagaEventType.ParentSagaSuccessResponded, ctx.Instance);
                        await exceptionProcessor.CheckProceedToFinallyAsync(ctx);
                    })
                    .Finalize(),
                When(FailedToSendSuccessToParentEvent)
                    .Then(ctx => this.Interceptor?.OnFailedToSendSuccessToParentEvent())
                    .IfAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        var innerException = FlowSagaUtil.GetExceptionFromFault(ctx);
                        var exception = new Exception(nameof(FailedToSendSuccessToParentEvent), innerException);
                        var flowTaskId = ctx.Data.Message.FlowTaskId.Value;
                        var flowTaskChunkIndex = ctx.Data.Message.FlowTaskChunkIndex.Value;
                        var shouldContinue = await ctx.Instance.FlowProcessor.ProcessExceptionAsync(flowTaskId, flowTaskChunkIndex, ctx, exception);
                        return !shouldContinue ? false : ctx.Instance.FlowContainer.ShouldFinalize();
                    }, x => x.Finalize()),
                When(FailedToSendEndSagaToChildEvent)
                    .Then(ctx => this.Interceptor?.OnFailedToSendEndSagaToChildEvent())
                    .ThenAsync(async ctx =>
                    {
                        SetupFlowInstance(ctx);
                        await logger.LogFlowSaga(FlowSagaEventType.ParentSagaFaultResponded, ctx.Instance);
                        await exceptionProcessor.CheckProceedToFinallyAsync(ctx);
                    })
                    .Finalize()
            );

            SetCompletedWhenFinalized();
        }

        private async Task ProcessStartSagaEvent(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaStartCommand<TFlowData>> ctx)
        {
            InitializeFlowInstance(ctx);

            var rabitMqConnectionString = this.rabbitMqConnectionStringProvider.ConnectionString;
            var faultAddress = FlowQueueUtil.GetUnifiedFaultAddress(rabitMqConnectionString);
            var correlationInfo = CorrelationUtil.GetCorrelationInfo(ctx);
            var startCommand = ctx.Data;
            var launchCommand = new FlowSagaLaunchCommand<TFlowData>(startCommand);
            var queueAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabitMqConnectionString);

            if (launchCommand.ParentSagaCorrelationId.HasValue || this.ShouldLaunchImmediately)
            {
                var endpoint = await ctx.GetSendEndpoint(queueAddress);

                await CorrelationUtil.SendWithCorrelationHeadersAsync(
                    endpoint, correlationInfo, launchCommand, faultAddress);

                return;
            }

            var unsafeSpace = UnsafeFlowSpaceWithNoContext<TFlowData, TSagaContext>.Create(ctx, rabitMqConnectionString);
            await DelayLaunch(unsafeSpace, launchCommand, queueAddress, faultAddress, correlationInfo);
        }

        private async Task ProcessLaunchSagaEvent(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaLaunchCommand<TFlowData>> ctx)
        {
            SetupFlowInstance(ctx);

            await TryGetCanStartWithLockAsync(ctx);

            var flowInstance = ctx.Instance;
            if (flowInstance.CanBeStarted)
            {
                await logger.LogFlowSaga(FlowSagaEventType.SagaStarted, flowInstance);
                await flowInstance.FlowProcessor.ProcessTasksAsync(ctx);
                return;
            }

            await logger.LogFlowSaga(FlowSagaEventType.SagaStartDelayed, flowInstance);

            var rabitMqConnectionString = this.rabbitMqConnectionStringProvider.ConnectionString;
            var correlationInfo = CorrelationUtil.GetCorrelationInfo(ctx);
            var queueAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabitMqConnectionString);
            var faultAddress = FlowQueueUtil.GetUnifiedFaultAddress(rabitMqConnectionString);
            var message = ctx.Data;

            BackgroundJob.Schedule<IScheduleMessageService>(
                 x => x.SendMessageAsync(correlationInfo, message, queueAddress, faultAddress),
                 canNotStartSagaRetryDelay);
        }

        private async Task TryGetCanStartWithLockAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaLaunchCommand<TFlowData>> ctx)
        {
            var flowInstance = ctx.Instance;
            var space = FinallySpace<TFlowData, TSagaContext>.Create(flowInstance, repository);
            try
            {
                flowInstance.CanBeStarted = await CanStartWithLockAsync(space);
            }
            catch (Exception e)
            {
                flowInstance.CanBeStarted = true;
                await logger.LogFlowSagaError(FlowSagaEventType.CanStartError, flowInstance, new[] { e });
            }
        }

        private void ConfigureMainAndFinallyFlows()
        {
            masterActiveFlow = masterMainFlow;
            ConfigureFlow();

            if (!masterMainFlow.AllFlowTasks.Any())
            {
                Do();
            }
            masterActiveFlow.DisableTasksAddition();

            masterActiveFlow = masterFinallyFlow;
            masterFinallyFlow.FirstTaskId = masterMainFlow.GetNextTaskId() - 1;
            ConfigureFinallyFlow();

            if (!masterFinallyFlow.AllFlowTasks.Any())
            {
                Do();
            }
            masterActiveFlow.DisableTasksAddition();

            masterActiveFlow = masterMainFlow;
        }

        private FlowTaskChain<TFlowData, TSagaContext> Do(string name, DoFuncDelegate<TFlowData, TSagaContext> func, bool isFlowEngineTask = false)
        {
            var callerMethodName = new StackTrace().GetFrame(2).GetMethod().Name;
            var taskDelegate = CreateAsyncTaskDelegate(func);
            var logLevel = isFlowEngineTask ? FlowLogLevel.Extended : FlowLogLevel.Basic;
            var task = new FlowTask<TFlowData, TSagaContext>(masterActiveFlow, taskDelegate, logger, logLevel)
            {
                CallerMethodName = callerMethodName
            };
            task.Name = name ?? task.Id.ToString();
            return new FlowTaskChain<TFlowData, TSagaContext>(task);
        }

        private FlowTaskDelegate<TFlowData, TSagaContext> CreateAsyncTaskDelegate(DoFuncDelegate<TFlowData, TSagaContext> func)
        {
            return async (ctx, t) =>
            {
                var space = FinallySpace<TFlowData, TSagaContext>.Create(ctx.Instance, repository);
                await func(space);
                ctx.Instance.FlowData = space.Data;
                t.StatesContainer.SetAll(FlowTaskState.Completed);
                await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, ctx.Instance);
            };
        }

        private FlowTaskDelegate<TFlowData, TSagaContext> CreateAsyncTaskDelegate(DoUnsafeFuncDelegate<TFlowData, TSagaContext> func)
        {
            return async (ctx, t) =>
            {
                var space = UnsafeFinallyFlowSpace<TFlowData, TSagaContext>.Create(
                    ctx, repository, rabbitMqConnectionStringProvider.ConnectionString);

                await func(space);
                ctx.Instance.FlowData = space.Data;
                t.StatesContainer.SetAll(FlowTaskState.Completed);

                await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, t, ctx.Instance);
            };
        }

        private void SetupFlowInstance(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            var flowInstance = ctx.Instance;
            flowInstance.FlowContainer = FlowContainer<TFlowData, TSagaContext>.Create(
                masterMainFlow,
                masterFinallyFlow,
                flowInstance,
                this.ShouldIngonreMainFailed);

            flowInstance.FlowProcessor = new FlowProcessor<TFlowData, TSagaContext>(
                rabbitMqConnectionStringProvider,
                this.exceptionProcessor,
                repository,
                flowInstance.FlowContainer,
                logger);
        }

        private void InitializeFlowInstance(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, FlowSagaStartCommand<TFlowData>> ctx)
        {
            SetupFlowInstance(ctx);

            var flowInstance = ctx.Instance;
            var command = ctx.Data;
            var parentSagaAddress = ctx.CreateConsumeContext().SourceAddress.AbsoluteUri;

            flowInstance.UserId = command.UserId;
            flowInstance.UserEmail = command.UserEmail;
            var parentSagaCorrelationId = command.ParentSagaCorrelationId;
            if (parentSagaCorrelationId.HasValue)
            {
                flowInstance.ParentSagaAddress = parentSagaAddress;
                flowInstance.ParentSagaCorrelationId = parentSagaCorrelationId;
                flowInstance.ParentSagaFlowTaskId = command.FlowTaskId;
                flowInstance.ParentSagaChunkIndex = command.FlowTaskChunkIndex;
            }

            flowInstance.FlowData = command.FlowData;
        }
    }
}
