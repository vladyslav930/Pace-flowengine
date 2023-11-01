using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger.Persistence
{
    public interface IMongoLoggerRepository
    {
        Task InsertAsync<TEntity>(TEntity entity, string collectionName, params KeyValuePair<string, object>[] additionalProperties);
    }
}
