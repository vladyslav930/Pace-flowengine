using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl
{
    internal class FlowContainer<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public readonly Flow<TFlowData, TSagaContext> MainFlow;
        public readonly Flow<TFlowData, TSagaContext> FinallyFlow;

        public Flow<TFlowData, TSagaContext> ActiveFlow => MainFlow.IsFinished ? FinallyFlow : MainFlow;

        public FlowContainer(bool shouldIngonreMainFailed)
        {
            MainFlow = new Flow<TFlowData, TSagaContext>(shouldIngonreMainFailed);
            FinallyFlow = new Flow<TFlowData, TSagaContext>(shouldIngonreFailed: false);
        }

        public List<FlowTaskStateException> GetAllExceptions() => GetMainFlowExceptions().Concat(GetFinallyFlowExceptions()).ToList();

        public List<FlowTaskStateException> GetMainFlowExceptions() => MainFlow.GetExceptions().ToList();

        public List<FlowTaskStateException> GetFinallyFlowExceptions() => FinallyFlow.GetExceptions().ToList();

        public bool IsMainSuccessAndFinallySuccess()
        {
            return this.MainFlow.IsCompletedSuccessfully && this.FinallyFlow.IsCompletedSuccessfully;;
        }

        public bool IsMainSuccessAndFinallyFailed()
        {
            return this.MainFlow.IsCompletedSuccessfully
                && this.FinallyFlow.IsFailed;
        }

        public bool IsMainFailedAndFinallyIsInInitialState()
        {
            return this.MainFlow.IsFailed 
                && this.FinallyFlow.IsInInitialState;
        }

        public bool IsMainFailedAndFinallyIsStarted()
        {
            return this.MainFlow.IsFailed && this.FinallyFlow.HasPendingTasks;
        }

        public bool IsMainFailedAndFinallySuccess()
        {
            return this.MainFlow.IsFailed && this.FinallyFlow.IsCompletedSuccessfully;
        }

        public bool IsMainFailedAndFinallyFailed()
        {
            return this.MainFlow.IsFailed && this.FinallyFlow.IsFailed;
        }

        public bool ShouldFinalize()
        {
            return this.MainFlow.IsFinished && this.FinallyFlow.IsFinished;
        }

        public bool ShouldRequestParentSaga(FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            if (flowInstance.IsSelfDependent
                || flowInstance.IsParentSagaRequested) return false;

            return ShouldFinalize();
        }

        public void UpdateFlowInstance(FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            flowInstance.MainFlowTaskStateItems = MainFlow.GetTaskStateItems();
            flowInstance.FinallyFlowTaskStateItems = FinallyFlow.GetTaskStateItems();
        }

        public string Serialize()
        {
            var mainFlowJson = JsonConvert.SerializeObject(MainFlow);
            var finallyFlowJson = JsonConvert.SerializeObject(FinallyFlow);
            var str = Environment.NewLine + mainFlowJson + Environment.NewLine + finallyFlowJson;
            return str;
        }

        public static FlowContainer<TFlowData, TSagaContext> Create(
            Flow<TFlowData, TSagaContext> masterMainFlow,
            Flow<TFlowData, TSagaContext> masterFinallyFlow,
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            bool shouldIngonreMainFailed)
        {
            var container = new FlowContainer<TFlowData, TSagaContext>(shouldIngonreMainFailed);
            foreach (var flowTask in masterMainFlow.AllFlowTasks)
                flowTask.CloneIntoFlow(container.MainFlow);

            foreach (var flowTask in container.MainFlow.AllFlowTasks)
                flowTask.SetPreviousTasks(masterMainFlow.AllFlowTasks);

            foreach (var flowTask in masterFinallyFlow.AllFlowTasks)
                flowTask.CloneIntoFlow(container.FinallyFlow);

            foreach (var flowTask in container.FinallyFlow.AllFlowTasks)
                flowTask.SetPreviousTasks(masterFinallyFlow.AllFlowTasks);

            container.MainFlow.UpdateTaskStates(flowInstance.MainFlowTaskStateItems);
            container.FinallyFlow.UpdateTaskStates(flowInstance.FinallyFlowTaskStateItems);

            container.MainFlow.DisableTasksAddition();
            container.FinallyFlow.DisableTasksAddition();
            return container;
        }

        public bool CanStartTasks => !ActiveFlow.IsFailed;
    }
}
