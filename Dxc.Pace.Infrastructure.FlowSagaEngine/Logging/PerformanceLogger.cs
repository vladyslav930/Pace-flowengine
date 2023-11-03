using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger.Persistence;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public class PerformanceLogger : IPerformanceLogger
    {
        private const string FinFactorsPerformanceCollectionName = "FinFactorsPerformance";
        private const string AllocationsPerformanceCollectionName = "AllocationsPerformance";
        private const string ReportDataSourceBasePerformanceCollectionName = "ReportDataSourceBasePerformance";

        private readonly IMongoLoggerRepository _mongoLoggerRepository;

        public PerformanceLogger(IMongoLoggerRepository mongoLoggerRepository)
        {
            _mongoLoggerRepository = mongoLoggerRepository;
        }

        public Task LogFinFactorsPerformance<TPerformance>(TPerformance performance)
        {
            return _mongoLoggerRepository.InsertAsync(performance, FinFactorsPerformanceCollectionName);
        }

        public Task LogAllocationsPerformance<TPerformance>(TPerformance performance)
        {
            return _mongoLoggerRepository.InsertAsync(performance, AllocationsPerformanceCollectionName);
        }

        public Task ReportDataSourceBasePerformance<TPerformance>(TPerformance performance)
        {
            return _mongoLoggerRepository.InsertAsync(performance, ReportDataSourceBasePerformanceCollectionName);
        }
    }
}
