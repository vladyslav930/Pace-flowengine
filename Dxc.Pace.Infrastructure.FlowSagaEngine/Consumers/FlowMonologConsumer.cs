using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{

    public abstract class FlowMonologConsumer<TRequest, TSagaContext> : FlowConsumerBase<TRequest, TSagaContext>
        where TRequest : class
        where TSagaContext : class
    {
        public FlowMonologConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected abstract Task DoAsync(TRequest data, TSagaContext context);

        public override async Task DoConsumeAsync(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            var context = FlowSagaContextUtil.CreateSagaContext<TSagaContext>(ctx.Message.CorrelationId, Repository);
            await DoAsync(ctx.Message.Data, context);
            await SendResponseAsync(ctx);
        }
    }

    public abstract class FlowMonologConsumer<TRequest> : FlowConsumerBase<TRequest, object>
        where TRequest : class
    {
        public FlowMonologConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected abstract Task DoAsync(TRequest data);

        public override async Task DoConsumeAsync(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            await DoAsync(ctx.Message.Data);
            await SendResponseAsync(ctx);
        }
    }
}
