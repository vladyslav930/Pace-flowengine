using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext
{
    public interface IFlowSagaContextValue<T>
    {
        Task<T> GetAsync();
        Task SetAsync(T value, TimeSpan? expiresIn = null);

        string GetKey();

        Task<bool> ExistsAsync();
    }
}
