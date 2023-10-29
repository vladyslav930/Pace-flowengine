using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTask<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        internal static FlowTaskStateContainer GetInitialStatesContainer() =>
            new FlowTaskStateContainer()
            {
                ChunkStates = new List<FlowTaskChunkState>() {
                    new FlowTaskChunkState { State = FlowTaskState.Initial }
                }
            };

        private readonly FlowSagaLogger<TFlowData, TSagaContext> logger;
        private readonly FlowTaskDelegate<TFlowData, TSagaContext> taskDelegate;

        internal readonly FlowTaskCompleteDelegate<TFlowData, TSagaContext> CompleteTaskDelegate;
        internal readonly FlowTaskCompleteChunkDelegate<TFlowData, TSagaContext> CompleteChunkTaskDelegate;
        internal readonly FlowLogLevel LogLevel;

        internal readonly List<FlowTask<TFlowData, TSagaContext>> PreviousTasks = new List<FlowTask<TFlowData, TSagaContext>>();
        internal FlowTaskCheckConditionDelegate<TFlowData, TSagaContext> DoCondition;

        internal readonly Flow<TFlowData, TSagaContext> flow;
        internal int Id;
        internal string Name;
        internal string CallerMethodName;

        internal FlowTaskStateContainer StatesContainer { get; set; } = GetInitialStatesContainer();

        internal bool IsCompletedSuccessfully => StatesContainer.CheckAll(FlowTaskState.Completed, FlowTaskState.Cancelled);
        internal bool IsInInitialState => StatesContainer.CheckAll(FlowTaskState.Initial);
        internal bool IsProcessed => StatesContainer.CheckAll(FlowTaskState.Completed, FlowTaskState.Cancelled, FlowTaskState.Failed);
        internal bool IsPending => StatesContainer.CheckAny(FlowTaskState.Started);
        internal bool IsFailed => StatesContainer.CheckAny(FlowTaskState.Failed);
        internal bool CanBeStarted => StatesContainer.CheckAll(FlowTaskState.Initial)
                && PreviousTasks.All(x => x.IsProcessed);

        internal FlowTask(
            Flow<TFlowData, TSagaContext> flow,
            FlowTaskDelegate<TFlowData, TSagaContext> taskDelegate,
            FlowSagaLogger<TFlowData, TSagaContext> logger,
            FlowLogLevel logLevel = FlowLogLevel.Basic)
        {
            this.taskDelegate = taskDelegate;
            this.logger = logger;
            this.LogLevel = logLevel;
            this.flow = flow;
            Id = this.flow.GetNextTaskId();
            this.flow.AddTask(this);
        }

        internal FlowTask(
            Flow<TFlowData, TSagaContext> flow,
            FlowTaskDelegate<TFlowData, TSagaContext> taskDelegate,
            FlowTaskCompleteDelegate<TFlowData, TSagaContext> completeTaskDelegate,
            FlowTaskCompleteChunkDelegate<TFlowData, TSagaContext> completeChunkTaskDelegate,
            FlowSagaLogger<TFlowData, TSagaContext> logger,
            FlowLogLevel logLevel = FlowLogLevel.Basic) : this(flow, taskDelegate, logger, logLevel)
        {
            CompleteTaskDelegate = completeTaskDelegate;
            CompleteChunkTaskDelegate = completeChunkTaskDelegate;
        }

        public override int GetHashCode() => System.HashCode.Combine(Id);

        public override bool Equals(object obj)
        {
            return obj is FlowTask<TFlowData, TSagaContext> other
                && this.Id.Equals(other.Id);
        }

        internal async Task StartTaskAsync(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx, IFlowSagaContextRepository repository)
        {
            if (DoCondition != null)
            {
                var space = FinallySpace<TFlowData, TSagaContext>.Create(ctx.Instance, repository);
                var shouldStart = await DoCondition(space);
                if (!shouldStart)
                {
                    await Cancel(ctx);
                    var alreadyCanceledTasks = new List<FlowTask<TFlowData, TSagaContext>>() { this };
                    await this.CancelNextTasks(ctx, alreadyCanceledTasks);
                    return;
                }
            }

            await logger.LogFlowTask(FlowTaskEventType.TaskStarted, this, ctx.Instance);

            StatesContainer.SetAll(FlowTaskState.Started);
            await taskDelegate(ctx, this);
        }

        internal async Task CompleteTaskAsync(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            int flowTaskChunkIndex,
            FlowResponse flowResponse)
        {
            StatesContainer[flowTaskChunkIndex].State = FlowTaskState.Completed;
            if (this.CompleteChunkTaskDelegate != null)
            {
                await this.CompleteChunkTaskDelegate(ctx, flowResponse, this, flowTaskChunkIndex);
            }

            if (this.IsProcessed)
            {
                if (CompleteTaskDelegate != null)
                {
                    await CompleteTaskDelegate(ctx, this);
                }

                await logger.LogFlowTask(FlowTaskEventType.TaskCompleted, this, ctx.Instance);
            }
        }

        internal FlowTask<TFlowData, TSagaContext> GetConditionTask(FlowTaskCheckConditionDelegate<TFlowData, TSagaContext> doCondition)
        {
            var conditionTask = GetJoinTask("Condition Task");
            conditionTask.DoCondition = doCondition;
            return conditionTask;
        }

        internal FlowTask<TFlowData, TSagaContext> GetBypassTask()
        {
            var bypassTask = GetJoinTask("Bypass Task");
            return bypassTask;
        }

        internal FlowTask<TFlowData, TSagaContext> GetJoinTask(string taskName)
        {
            var joinTask = new FlowTask<TFlowData, TSagaContext>(flow, (ctx, t) =>
            {
                t.StatesContainer.SetAll(FlowTaskState.Completed);
                return Task.CompletedTask;
            }, logger, FlowLogLevel.Extended);
            joinTask.Name = taskName + " " + joinTask.Id;
            return joinTask;
        }

        internal void SetPreviousTasks(List<FlowTask<TFlowData, TSagaContext>> allFlowTasks)
        {
            var originalTask = allFlowTasks.Single(x => x.Id == this.Id);

            foreach (var originalPreviousTask in originalTask.PreviousTasks)
            {
                var clonePreviousTask = this.flow.AllFlowTasks.Single(x => x.Id == originalPreviousTask.Id);
                this.PreviousTasks.Add(clonePreviousTask);
            }
        }

        internal FlowTask<TFlowData, TSagaContext> CloneIntoFlow(Flow<TFlowData, TSagaContext> flow)
        {
            var clone = new FlowTask<TFlowData, TSagaContext>(
                flow,
                this.taskDelegate,
                this.CompleteTaskDelegate,
                this.CompleteChunkTaskDelegate,
                this.logger,
                this.LogLevel)
            {
                Id = this.Id,
                Name = this.Name,
                CallerMethodName = this.CallerMethodName,
                DoCondition = this.DoCondition,
            };
            return clone;
        }

        internal List<FlowTaskStateException> GetExceptions() => StatesContainer.GetExceptions();

        private async Task CancelNextTasks(
            BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx,
            List<FlowTask<TFlowData, TSagaContext>> alreadyCanceledTasks)
        {
            var newCanceledTasks = new List<FlowTask<TFlowData, TSagaContext>>();
            var nextTasks = GetNextTasks();
            foreach (var nextTask in nextTasks)
            {
                if (nextTask.ShouldCancel(alreadyCanceledTasks))
                {
                    await nextTask.Cancel(ctx);
                    newCanceledTasks.Add(nextTask);
                }
            }

            foreach (var newCanceledTask in newCanceledTasks)
                alreadyCanceledTasks.Add(newCanceledTask);

            foreach (var newCanceledTask in newCanceledTasks)
            {
                await newCanceledTask.CancelNextTasks(ctx, alreadyCanceledTasks);
            }
        }

        private bool ShouldCancel(List<FlowTask<TFlowData, TSagaContext>> alreadyCanceledTasks)
        {
            return PreviousTasks.All(x => alreadyCanceledTasks.Any(y => y.Id == x.Id));
        }

        private Task Cancel(BehaviorContext<FlowInstance<TFlowData, TSagaContext>, object> ctx)
        {
            StatesContainer.SetAll(FlowTaskState.Cancelled);
            return logger.LogFlowTask(FlowTaskEventType.TaskCancelled, this, ctx.Instance);
        }

        private List<FlowTask<TFlowData, TSagaContext>> GetNextTasks()
        {
            var nextTasks = flow.AllFlowTasks
                .Where(x => x.PreviousTasks.Any(y => y.Id == this.Id))
                .ToList();

            return nextTasks;
        }

    }
}
