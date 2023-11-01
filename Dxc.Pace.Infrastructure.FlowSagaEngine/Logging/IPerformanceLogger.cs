using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public interface IPerformanceLogger
    {
        Task LogFinFactorsPerformance<TPerformance>(TPerformance performance);
        Task LogAllocationsPerformance<TPerformance>(TPerformance performance);
        Task ReportDataSourceBasePerformance<TPerformance>(TPerformance performance);
    }
}
