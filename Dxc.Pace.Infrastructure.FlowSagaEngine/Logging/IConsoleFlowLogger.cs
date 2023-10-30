using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public interface IConsoleFlowLogger : IFlowSagaLogger, IFlowConsumerLogger
    {
    }
}
