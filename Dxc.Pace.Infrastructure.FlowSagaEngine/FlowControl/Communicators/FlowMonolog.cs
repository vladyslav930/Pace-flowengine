using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Spaces;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Communicators
{
    public class FlowMonolog<TFlowData, TSagaContext, TRequest>
            where TFlowData : class, new()
            where TSagaContext : class
            where TRequest : class
    {
        internal FlowMonolog() { }

        public string Name { get; set; }
        internal string CallerMethodName { get; set; }
        internal RequestsFactoryDelegate<TFlowData, TSagaContext, TRequest> RequestFactories { get; set; }
        internal FlowTaskCheckConditionDelegate<TFlowData, TSagaContext> WaitCondition { get; set; }
        internal FlowTaskCheckConditionDelegate<TFlowData, TSagaContext> SendCondition { get; set; }

        public void SetRequestFactory(Func<FlowSpace<TFlowData, TSagaContext>, Task<TRequest>> func)
        {
            RequestFactories = async s =>
            {
                var request = new FlowRequest<TRequest>
                {
                    Data = await func(s)
                };
                return new List<FlowRequest<TRequest>>() { request };
            };
        }

        public void SetRequestFactory(Func<FlowSpace<TFlowData, TSagaContext>, TRequest> func)
        {
            RequestFactories = s =>
            {
                var request = new FlowRequest<TRequest>
                {
                    Data = func(s)
                };
                var requests = new List<FlowRequest<TRequest>>() { request };
                return Task.FromResult(requests);
            };
        }

        public void SetRequestsFactory(Func<FlowSpace<TFlowData, TSagaContext>, IEnumerable<TRequest>> func)
        {
            RequestFactories = s =>
            {
                var requestDataItems = func(s);
                var requests = requestDataItems
                    .Select(x => new FlowRequest<TRequest> { Data = x })
                    .ToList();
                return Task.FromResult(requests);
            };
        }

        public void SetRequestsFactory(Func<FlowSpace<TFlowData, TSagaContext>, Task<IEnumerable<TRequest>>> func)
        {
            RequestFactories = async s =>
            {
                var requestDataItems = await func(s);
                var requests = requestDataItems
                    .Select(x => new FlowRequest<TRequest> { Data = x })
                    .ToList();
                return requests;
            };
        }

        public void SetSendCondition(Func<FlowSpace<TFlowData, TSagaContext>, bool> func)
        {
            SendCondition = s => { return Task.FromResult(func(s)); };
        }

        public void SetSendCondition(Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> func)
        {
            SendCondition = async s => { return await func(s); };
        }

        public void SetWaitCondition(Func<FlowSpace<TFlowData, TSagaContext>, bool> func)
        {
            WaitCondition = s => { return Task.FromResult(func(s)); };
        }

        public void SetWaitCondition(Func<FlowSpace<TFlowData, TSagaContext>, Task<bool>> func)
        {
            WaitCondition = async s => { return await func(s); };
        }
    }
}
