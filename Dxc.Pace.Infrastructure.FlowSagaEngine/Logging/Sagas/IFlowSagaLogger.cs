using System;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas
{
    public interface IFlowSagaLogger
    {
        Task LogFlowSaga(FlowSagaLogInfo info);
        Task LogFlowTask<TFlowData>(FlowTaskLogInfo<TFlowData> info) where TFlowData : class, new();
        Task LogFlowRequest<TRequest>(FlowRequestLogInfo<TRequest> info) where TRequest : class;
        Task LogData<TCustomData>(TCustomData customData, Guid correlationId);
    }
}
