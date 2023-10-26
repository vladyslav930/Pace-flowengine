using Automatonymous;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using MassTransit.Saga;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances
{
    public class FlowInstance<TFlowData, TSagaContext> : SagaStateMachineInstance, ISagaVersion
        where TFlowData : class, new()
        where TSagaContext : class
    {
        [IgnoreDataMember]
        internal FlowContainer<TFlowData, TSagaContext> FlowContainer;
        [IgnoreDataMember]
        internal FlowProcessor<TFlowData, TSagaContext> FlowProcessor;
        [IgnoreDataMember]
        public bool CanBeStarted;

        public string CurrentState { get; set; }
        public int Version { get; set; }
        public Guid CorrelationId { get; set; }
        public Guid? UserId { get; set; }
        public string UserEmail { get; set; }
        public Guid? ParentSagaCorrelationId { get; set; }
        public int? ParentSagaFlowTaskId { get; set; }
        public int? ParentSagaChunkIndex { get; set; }
        public string ParentSagaAddress { get; set; }
        public bool IsParentSagaRequested { get; set; }
        public bool IsDependent => ParentSagaCorrelationId.HasValue;
        public bool IsSelfDependent => !IsDependent;

        [IgnoreDataMember]
        public TFlowData FlowData
        {
            get
            {
                if (string.IsNullOrEmpty(FlowDataJson)) return new TFlowData();
                return JsonConvert.DeserializeObject<TFlowData>(FlowDataJson);
            }
            set
            {
                FlowDataJson = JsonConvert.SerializeObject(value);
            }
        }
        [IgnoreDataMember]
        public List<FlowTaskStateItem> MainFlowTaskStateItems
        {
            get
            {
                if (string.IsNullOrEmpty(MainFlowTaskStatesJson)) return new List<FlowTaskStateItem>();
                return JsonConvert.DeserializeObject<List<FlowTaskStateItem>>(MainFlowTaskStatesJson);
            }
            set
            {
                MainFlowTaskStatesJson = JsonConvert.SerializeObject(value);
            }
        }
        [IgnoreDataMember]
        public List<FlowTaskStateItem> FinallyFlowTaskStateItems
        {
            get
            {
                if (string.IsNullOrEmpty(FinallyFlowTaskStatesJson)) return new List<FlowTaskStateItem>();
                return JsonConvert.DeserializeObject<List<FlowTaskStateItem>>(FinallyFlowTaskStatesJson);
            }
            set
            {
                FinallyFlowTaskStatesJson = JsonConvert.SerializeObject(value);
            }
        }

        public string FlowDataJson { get; set; }
        public string MainFlowTaskStatesJson { get; set; }
        public string FinallyFlowTaskStatesJson { get; set; }

        public void UpdateTasksStates()
        {
            this.MainFlowTaskStateItems = this.FlowContainer.MainFlow.GetTaskStateItems();
            this.FinallyFlowTaskStateItems = this.FlowContainer.FinallyFlow.GetTaskStateItems();
        }
    }
}
