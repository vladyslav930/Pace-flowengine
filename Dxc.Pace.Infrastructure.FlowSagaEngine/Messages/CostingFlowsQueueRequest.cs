using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class CostingFlowsQueueRequest
    {
        public Guid QueueId { get; set; }
        public int CostingVersionId { get; set; }
        public Guid CorrelationId { get; set; }
        public object TargetLaunchCommand { get; set; }
        public string TargetLaunchCommandType { get; set; }
    }
}
