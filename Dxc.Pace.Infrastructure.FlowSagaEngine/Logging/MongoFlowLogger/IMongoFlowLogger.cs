using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger
{
    public interface IMongoFlowLogger : IFlowSagaLogger, IFlowConsumerLogger
    {
    }
}
