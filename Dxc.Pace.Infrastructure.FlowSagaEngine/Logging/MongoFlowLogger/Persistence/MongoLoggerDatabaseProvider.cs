using MongoDB.Driver;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger.Persistence
{
    public class MongoLoggerDatabaseProvider
    {
        private readonly IMongoDatabase _database;

        public MongoLoggerDatabaseProvider(string connectionString)
        {
            var url = new MongoUrl(connectionString);
            var settings = MongoClientSettings.FromUrl(url);

            _database = new MongoClient(settings).GetDatabase(url.DatabaseName);
        }

        public IMongoDatabase GetDatabase()
        {
            return _database;
        }
    }
}
