using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl
{

    internal class Flow<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        private readonly List<FlowTask<TFlowData, TSagaContext>> allFlowTasks = new List<FlowTask<TFlowData, TSagaContext>>();
        private readonly bool shouldIngonreFailed;
        private bool canAddTasks = true;

        public int FirstTaskId = 0;

        public List<FlowTask<TFlowData, TSagaContext>> AllFlowTasks => this.allFlowTasks;

        public bool IsProcessed => AllFlowTasks.All(x => x.IsProcessed);
        public bool IsFinished => !this.HasPendingTasks && (this.IsFailed || this.IsProcessed);
        public bool HasPendingTasks => AllFlowTasks.Any(x => x.IsPending);
        public bool IsFailed => !shouldIngonreFailed && AllFlowTasks.Any(x => x.IsFailed);
        public bool IsInInitialState => AllFlowTasks.All(x => x.IsInInitialState);
        public bool IsCompletedSuccessfully => this.shouldIngonreFailed 
            ? IsProcessed
            : AllFlowTasks.All(x => x.IsCompletedSuccessfully);

        public Flow(bool shouldIngonreFailed)
        {
            this.shouldIngonreFailed = shouldIngonreFailed;
        }

        public void AddTask(FlowTask<TFlowData, TSagaContext> flowTask)
        {
            if (!this.canAddTasks) throw new System.InvalidOperationException(FlowConstants.CanNotAddFlowTaskMessage);

            allFlowTasks.Add(flowTask);
        }

        public void DisableTasksAddition()
        {
            this.canAddTasks = false;
        }

        public void UpdateTaskStates(List<FlowTaskStateItem> stateItems)
        {
            foreach (var task in AllFlowTasks)
            {
                var item = stateItems.FirstOrDefault(x => x.TaskId == task.Id);
                if (item == null)
                {
                    task.StatesContainer = FlowTask<TFlowData, TSagaContext>.GetInitialStatesContainer();
                }
                else
                {
                    task.StatesContainer = item.StatesContainer;
                }
            }
        }

        public List<FlowTaskStateItem> GetTaskStateItems()
        {
            var items = AllFlowTasks.Select(x => new FlowTaskStateItem()
            {
                TaskId = x.Id,
                StatesContainer = x.StatesContainer,
            });

            return new List<FlowTaskStateItem>(items);
        }

        public List<FlowTask<TFlowData, TSagaContext>> GetTasksToStart()
        {
            var flowTasksToStart = AllFlowTasks
                .Where(x => x.CanBeStarted)
                .OrderBy(x => x.Id)
                .ToList();

            return flowTasksToStart;
        }

        public int GetNextTaskId()
        {
            var maxId = AllFlowTasks
                .Select(x => x.Id)
                .DefaultIfEmpty(FirstTaskId)
                .Max();

            return maxId + 1;
        }

        public List<FlowTaskStateException> GetExceptions() => AllFlowTasks.SelectMany(x => x.GetExceptions()).ToList();
    }
}
