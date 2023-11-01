using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers
{
    public interface IFlowConsumerLogger
    {
        Task LogFlowConsumer<TRequest>(FlowConsumerLogInfo<TRequest> info) where TRequest : class;
    }
}
