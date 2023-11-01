using System;
using System.Collections.Generic;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger.Persistence;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger
{
    public class MongoFlowLogger : IMongoFlowLogger
    {
        private const string FlowConsumersCollectionName = "FlowConsumers";
        private const string FlowSagasCollectionName = "FlowSagas";
        private const string FlowTasksCollectionName = "FlowTasks";
        private const string FlowRequestsCollectionName = "FlowRequests";
        private const string FlowCustomCollectionName = "FlowCustom";

        private readonly IMongoLoggerRepository _mongoLoggerRepository;

        private const string CorrelationIdPropertyName = "CorrelationId";

        public MongoFlowLogger(IMongoLoggerRepository mongoLoggerRepository)
        {
            _mongoLoggerRepository = mongoLoggerRepository;
        }

        public Task LogFlowConsumer<TRequest>(FlowConsumerLogInfo<TRequest> info) where TRequest : class
        {
            return _mongoLoggerRepository.InsertAsync(info, FlowConsumersCollectionName);
        }

        public Task LogFlowRequest<TRequest>(FlowRequestLogInfo<TRequest> info) where TRequest : class
        {
            return _mongoLoggerRepository.InsertAsync(info, FlowRequestsCollectionName);
        }

        public Task LogData<TCustomData>(TCustomData customData, Guid correlationId)
        {
            var additionalProperties = correlationId != null
                ? new[] {new KeyValuePair<string, object>(CorrelationIdPropertyName, correlationId)}
                : Array.Empty<KeyValuePair<string, object>>();
            return _mongoLoggerRepository.InsertAsync(new CustomData<TCustomData>(customData), FlowCustomCollectionName, additionalProperties);
        }

        public Task LogFlowSaga(FlowSagaLogInfo info)
        {
            return _mongoLoggerRepository.InsertAsync(info, FlowSagasCollectionName);
        }

        public Task LogFlowTask<TFlowData>(FlowTaskLogInfo<TFlowData> info) where TFlowData : class, new()
        {
            return _mongoLoggerRepository.InsertAsync(info, FlowTasksCollectionName);
        }
    }
}
