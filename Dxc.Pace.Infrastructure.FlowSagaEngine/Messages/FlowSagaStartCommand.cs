using MassTransit;
using Newtonsoft.Json;
using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Messages
{
    public class FlowSagaStartCommand<TFlowData> : IFlowSagaStartCommand
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public string FlowDataTypeFullName { get; }
        public Guid? ParentSagaCorrelationId { get; set; }
        public TFlowData FlowData { get; set; }
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; }
        public int? FlowTaskId { get; set; }
        public int? FlowTaskChunkIndex { get; set; }

        [JsonConstructor]
        public FlowSagaStartCommand(Guid? userId, string userEmail)
        {
            UserId = userId;
            UserEmail = userEmail;
            FlowDataTypeFullName = FlowData?.GetType().FullName;
        }

        public FlowSagaStartCommand(FlowSagaStartCommand<TFlowData> startCommand)
            : this(startCommand.UserId, startCommand.UserEmail)
        {
            ParentSagaCorrelationId = startCommand.ParentSagaCorrelationId;
            FlowData = startCommand.FlowData;
            FlowTaskId = startCommand.FlowTaskId;
            FlowTaskChunkIndex = startCommand.FlowTaskChunkIndex;
        }
    }
}
