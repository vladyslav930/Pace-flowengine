using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTaskChain<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        internal FlowTask<TFlowData, TSagaContext> StartTask;
        internal FlowTask<TFlowData, TSagaContext> FinalTask;

        internal FlowTaskChain() { }

        internal FlowTaskChain(FlowTask<TFlowData, TSagaContext> flowTask)
        {
            StartTask = flowTask;
            FinalTask = flowTask;
        }

        public FlowTaskChain<TFlowData, TSagaContext> ThenIf(
                    Func<FlowSpace<TFlowData, TSagaContext>, bool> doConditionFunc,
                    params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            Task<bool> doCondition(FlowSpace<TFlowData, TSagaContext> s)
            {
                return Task.FromResult(doConditionFunc(s));
            }
            return ThenIfInternal(doCondition, chains);
        }

        public FlowTaskChain<TFlowData, TSagaContext> ThenIf(
                    Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> doConditionFunc,
                    params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            return ThenIfInternal(doConditionFunc, chains);
        }

        public FlowTaskChain<TFlowData, TSagaContext> Then(params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            if (!chains.Any()) return this;

            if (chains.Length == 1)
            {
                var chain = chains.Single();
                chain.StartTask.PreviousTasks.Add(FinalTask);
                chain.StartTask = StartTask;
                return chain;
            }

            var startJoinTask = StartTask.GetJoinTask("Start Join Task");
            startJoinTask.PreviousTasks.Add(FinalTask);

            var finalJoinTask = StartTask.GetJoinTask("Final Join Task");

            var joinChain = new FlowTaskChain<TFlowData, TSagaContext>
            {
                StartTask = StartTask,
                FinalTask = finalJoinTask
            };

            foreach (var chain in chains)
            {
                chain.StartTask.PreviousTasks.Add(startJoinTask);
                finalJoinTask.PreviousTasks.Add(chain.FinalTask);
            }

            return joinChain;
        }

        private FlowTaskChain<TFlowData, TSagaContext> ThenIfInternal(
                    Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> doConditionFunc,
                    params FlowTaskChain<TFlowData, TSagaContext>[] chains)
        {
            if (!chains.Any()) return this;

            var conditionTask = StartTask.GetConditionTask(async s => await doConditionFunc(s));
            var conditionChain = new FlowTaskChain<TFlowData, TSagaContext>(conditionTask);

            var joinedConditionChain = conditionChain.Then(chains);

            var bypassTask = StartTask.GetBypassTask();
            var bypassChain = new FlowTaskChain<TFlowData, TSagaContext>(bypassTask);

            return Then(joinedConditionChain, bypassChain);
        }
    }
}
