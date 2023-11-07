using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class FlowSagaLaunchCommand<TFlowData>
    {
        public Guid CorrelationId { get; set; }
        public string FlowDataTypeFullName { get; }
        public Guid? ParentSagaCorrelationId { get; set; }
        public string ParentSagaAddress { get; set; }
        public TFlowData FlowData { get; set; }
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; }
        public int? FlowTaskId { get; set; }
        public int? FlowTaskChunkIndex { get; set; }

        public FlowSagaLaunchCommand()
        {
        }

        public FlowSagaLaunchCommand(FlowSagaStartCommand<TFlowData> startCommand)
        {
            CorrelationId = startCommand.CorrelationId;
            FlowDataTypeFullName = startCommand.FlowDataTypeFullName;
            ParentSagaCorrelationId = startCommand.ParentSagaCorrelationId;
            FlowData = startCommand.FlowData;
            UserId = startCommand.UserId;
            UserEmail = startCommand.UserEmail;
            FlowTaskId = startCommand.FlowTaskId;
            FlowTaskChunkIndex = startCommand.FlowTaskChunkIndex;
        }
    }
}
