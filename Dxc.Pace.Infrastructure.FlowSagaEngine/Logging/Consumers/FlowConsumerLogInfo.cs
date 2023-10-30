using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using System.Collections.Generic;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers
{
    public class FlowConsumerLogInfo<TRequest> where TRequest : class
    {
        public FlowConsumerEventType EventType { get; set; }
        public FlowRequest<TRequest> Request { get; set; }
        public List<SagaContextCollectionMetric> CollectionMetrics { get; set; }
    }
}
