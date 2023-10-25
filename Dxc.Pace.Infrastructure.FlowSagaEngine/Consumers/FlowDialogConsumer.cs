using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{
    public abstract class FlowDialogConsumer<TRequest, TSagaContext, TResponse> : FlowConsumerBase<TRequest, TSagaContext>
        where TRequest : class
        where TSagaContext : class
        where TResponse : class
    {
        public FlowDialogConsumer(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected abstract Task<TResponse> GetResponseAsync(TRequest data, TSagaContext context);

        public override async Task DoConsumeAsync(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            var context = FlowSagaContextUtil.CreateSagaContext<TSagaContext>(ctx.Message.CorrelationId, Repository);
            var data = await GetResponseAsync(ctx.Message.Data, context);
            await SendResponseAsync(ctx, data);
        }
    }

    public abstract class FlowDialogConsumer<TRequest, TResponse> : FlowConsumerBase<TRequest, IEmptySagaContext>
        where TRequest : class
    {
        public FlowDialogConsumer(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected abstract Task<TResponse> GetResponseAsync(TRequest data);

        public override async Task DoConsumeAsync(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            var data = await GetResponseAsync(ctx.Message.Data);
            await SendResponseAsync(ctx, data);
        }
    }
}
