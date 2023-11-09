using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine
{
    public static class MicrosoftDependencyInjectionExtensions
    {
        public static IServiceCollection UseFlowSagaService<TFlowSagaRepository>(this IServiceCollection services)
            where TFlowSagaRepository : class, IFlowSagaContextRepository
        {
            services.AddScoped<IFlowSagaService, FlowSagaService>();
            services.AddTransient<IFlowSagaContextRepository, TFlowSagaRepository>();
            services.AddTransient<IPerformanceLogger, PerformanceLogger>();
            return services;
        }
    }
}
