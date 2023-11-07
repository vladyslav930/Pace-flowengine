using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class FlowRequest
    {
        public Guid CorrelationId { get; set; }
        public int FlowTaskId { get; set; }
        public int FlowTaskChunkIndex { get; set; }
        public bool DoNotSendResponse { get; set; }
    }

    public class FlowRequest<TData> : FlowRequest where TData : class
    {
        public TData Data { get; set; }
    }
}
