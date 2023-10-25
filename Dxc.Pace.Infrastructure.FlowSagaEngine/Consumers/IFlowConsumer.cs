using MassTransit;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{
    public interface IFlowConsumer<out TRequest, TSagaContext> : IConsumer 
        where TRequest : class
        where TSagaContext : class
    {
    }
}
