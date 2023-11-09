using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Services
{
    public interface IFlowSagaService
    {
        Task StartSagaAsync<TFlowData>(TFlowData data)
            where TFlowData : class, ILaunchSettings, new();            

        Task StartSagaAsync<TFlowData, TSagaContext>(TFlowData data, Func<TSagaContext, Task> contextProcessor, string emailPrincipal = null)
            where TFlowData : class, ILaunchSettings, new()
            where TSagaContext : class;                 

        Task StartSagaAsync<TFlowData, TSagaContext>(TFlowData data, Action<TSagaContext> contextProcessor)
            where TFlowData : class, ILaunchSettings, new()
            where TSagaContext : class;
    }
}
