using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces
{
    public class FinallySpace<TFlowData, TSagaContext> : FinallyFlowSpace<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        public List<Exception> FinallyFlowExceptions { get; internal set; }

        public string CurrentState { get; set; }
        public string MainFlowTaskStatesJson { get; internal set; }
        public string FinallyFlowTaskStatesJson { get; internal set; }

        internal FinallySpace() { }

        internal static FinallySpace<TFlowData, TSagaContext> Create(
                    IFlowSagaContextRepository repository)
        {
            var space = new FinallySpace<TFlowData, TSagaContext>();
            space.SagaContext = new Lazy<TSagaContext>(() =>
            {
                return FlowSagaContextUtil.CreateSagaContext<TSagaContext>(space.CorrelationId, repository);
            });

            return space;
        }

        internal static FinallySpace<TFlowData, TSagaContext> Create(
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            IFlowSagaContextRepository repository)
        {
            var space = Create(repository);
            space.CorrelationId = flowInstance.CorrelationId;
            space.UserId = flowInstance.UserId;
            space.UserEmail = flowInstance.UserEmail;
            space.ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId;
            space.Data = flowInstance.FlowData;

            TryCollectExceptions(flowInstance, space);

            space.CurrentState = flowInstance.CurrentState;
            space.MainFlowTaskStatesJson = flowInstance.MainFlowTaskStatesJson;
            space.FinallyFlowTaskStatesJson = flowInstance.FinallyFlowTaskStatesJson;

            return space;
        }

        private static void TryCollectExceptions(FlowInstance<TFlowData, TSagaContext> flowInstance, FinallySpace<TFlowData, TSagaContext> space)
        {
            try
            {
                space.MainFlowExceptions = flowInstance.FlowContainer
                    .GetMainFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList();
                space.FinallyFlowExceptions = flowInstance.FlowContainer
                    .GetFinallyFlowExceptions()
                    .Select(x => x.Exception)
                    .ToList();
            }
            catch (Exception ex)
            {
                // todo: sometimes here was the error: System.Exception, Message: "Collection was modified; enumeration operation may not execute."
                // at System.Collections.Generic.List`1.Enumerator.MoveNextRare() at System.Linq.Enumerable.SelectListIterator`2.MoveNext()
                // at System.Linq.Enumerable.ConcatIterator`1.MoveNext() at System.Linq.Enumerable.WhereEnumerableIterator`1.ToList()
                // at System.Linq.Enumerable.SelectManySingleSelectorIterator`2.ToList()

                Log.Error($"Error on {nameof(TryCollectExceptions)}", ex);
                space.MainFlowExceptions = new List<Exception>();
                space.FinallyFlowExceptions = new List<Exception>();
            }
        }
    }
}
