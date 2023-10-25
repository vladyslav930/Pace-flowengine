using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{
    public class FlowConsumerLogUtil
    {
        private readonly IFlowConsumerLogger logger;

        public FlowConsumerLogUtil(IFlowConsumerLogger logger)
        {
            this.logger = logger;
        }

        public Task LogStarted<TRequest>(FlowRequest<TRequest> request, List<SagaContextCollectionMetric> collectionMetrics)
            where TRequest : class
        {
            var info = new FlowConsumerLogInfo<TRequest>()
            {
                EventType = FlowConsumerEventType.ConsumerStarted,
                Request = request,
                CollectionMetrics = collectionMetrics
            };
            return logger.LogFlowConsumer(info);
        }

        public Task LogCompleted<TRequest>(FlowRequest<TRequest> request)
             where TRequest : class
        {
            var info = new FlowConsumerLogInfo<TRequest>()
            {
                EventType = FlowConsumerEventType.ConsumerCompleted,
                Request = request
            };
            return logger.LogFlowConsumer(info);
        }

        public Task LogError<TRequest>(Exception exception, FlowRequest<TRequest> request)
             where TRequest : class
        {
            var info = new FlowConsumerErrorLogInfo<TRequest>()
            {
                EventType = FlowConsumerEventType.ConsumerError,
                Request = request,
                Exception = ExceptionInfo.Create(exception)
            };
            return logger.LogFlowConsumer(info);
        }
    }
}
