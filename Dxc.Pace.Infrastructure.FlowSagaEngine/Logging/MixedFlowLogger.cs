using System;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public class MixedFlowLogger : IFlowSagaLogger, IFlowConsumerLogger
    {
        private readonly IConsoleFlowLogger _consoleFlowLogger;
        private readonly IMongoFlowLogger _mongoFlowLogger;

        public MixedFlowLogger(IConsoleFlowLogger consoleFlowLogger, IMongoFlowLogger mongoFlowLogger)
        {
            _consoleFlowLogger = consoleFlowLogger;
            _mongoFlowLogger = mongoFlowLogger;
        }

        public async Task LogFlowSaga(FlowSagaLogInfo info)
        {
            await _consoleFlowLogger.LogFlowSaga(info);
            await _mongoFlowLogger.LogFlowSaga(info);
        }

        public async Task LogFlowTask<TFlowData>(FlowTaskLogInfo<TFlowData> info) where TFlowData : class, new()
        {
            await _consoleFlowLogger.LogFlowTask(info);
            await _mongoFlowLogger.LogFlowTask(info);
        }

        public async Task LogFlowConsumer<TRequest>(FlowConsumerLogInfo<TRequest> info) where TRequest : class
        {
            await _consoleFlowLogger.LogFlowConsumer(info);
            await _mongoFlowLogger.LogFlowConsumer(info);
        }

        public async Task LogFlowRequest<TRequest>(FlowRequestLogInfo<TRequest> info) where TRequest : class
        {
            await _consoleFlowLogger.LogFlowRequest(info);
            await _mongoFlowLogger.LogFlowRequest(info);
        }

        public async Task LogData<TCustomData>(TCustomData customData, Guid correlationId)
        {
            await _consoleFlowLogger.LogData(customData, correlationId);
            await _mongoFlowLogger.LogData(customData, correlationId);
        }
    }
}
