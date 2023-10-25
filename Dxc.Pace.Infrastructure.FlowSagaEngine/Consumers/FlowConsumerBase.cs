using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.Core.Configuration.LogManager;
using Dxc.Pace.Infrastructure.MassTransit.Logging.Helpers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{
    public abstract class FlowConsumerBase<TRequest, TSagaContext>
        : IFlowConsumer<TRequest, TSagaContext>, IConsumer<FlowRequest<TRequest>>
        where TSagaContext : class
        where TRequest : class
    {
        private readonly ICorrelationLogManager correlationLogManager;
        private readonly FlowConsumerLogUtil logger;
        private readonly ConsumerSettings consumerSettings;

        protected IFlowSagaContextRepository Repository { get; }

        /// <summary>
        /// Can be called the number of times MaxCallTimes, otherwise only one call is permitted
        /// </summary>
        protected virtual bool IsIdempotent => false;
      
        protected virtual int? MaxCallTimes { get; private set; }

        public FlowConsumerBase(IServiceProvider serviceProvider)
        {
            this.Repository = serviceProvider.GetRequiredService<IFlowSagaContextRepository>();
            correlationLogManager = serviceProvider.GetRequiredService<ICorrelationLogManager>();
            logger = new FlowConsumerLogUtil(serviceProvider.GetRequiredService<IFlowConsumerLogger>());

            var consumerSettingsSection = serviceProvider.GetService<IOptions<ConsumerSettings>>();
            consumerSettings = consumerSettingsSection.Value;
            MaxCallTimes = !IsIdempotent ? 1 : MaxCallTimes.GetValueOrDefault(consumerSettings.MaxCallsIfIdempotent);
        }

        public abstract Task DoConsumeAsync(ConsumeContext<FlowRequest<TRequest>> ctx);

        public async Task Consume(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            var correlationInfo = CorrelationUtil.GetCorrelationInfo(ctx);
            correlationLogManager.JwtToken = correlationInfo.JwtToken;
            correlationLogManager.LogCorrelationId = correlationInfo.LogCorrelationId;
            correlationLogManager.CostingVersionId = correlationInfo.CostingVersionId;
            var request = ctx.Message;            

            try
            {
                var collectionMetrics = await GetSagaContextCollectionMetricsAsync(request.CorrelationId);
                await logger.LogStarted(request, collectionMetrics);

                if (!consumerSettings.DisableCheckForRepeatedCalls)
                {
                    await CheckDuplicateConsumerCallAsync(request);
                }

                await DoConsumeAsync(ctx);
                await logger.LogCompleted(request);
            }
            catch (Exception ex)
            {
                await logger.LogError(ex, request);

                var exceptionMessage = $"{ex.GetType().Name} in {this.GetType().FullName}, TaskId: {request.FlowTaskId}, InnerException: {ex}";
                var response = new FailedFlowResponse
                {
                    CorrelationId = request.CorrelationId,
                    FlowTaskId = request.FlowTaskId,
                    FlowTaskChunkIndex = request.FlowTaskChunkIndex,
                    ExceptionMessage = exceptionMessage
                };

                var endpoint = await ctx.GetSendEndpoint(ctx.FaultAddress);
                await endpoint.Send(response);
            }
        }

        private async Task CheckDuplicateConsumerCallAsync(FlowRequest<TRequest> request)
        {
            if (request.FlowTaskId == 0)
                return;     // FlowTask from QueueSagas

            var key = $"ConsumerCall_{request.CorrelationId}_{request.FlowTaskId}_{request.FlowTaskChunkIndex}";
            var callTimes = await this.Repository.GetAsync<int>(key);
            if (callTimes >= MaxCallTimes)
            {
                await Task.Delay(consumerSettings.WaitBeforeRaiseDuplicateConsumerException);
                throw new Exception($"Duplicate consumer call: {key}");
            }
            else
            {
                await this.Repository.SetAsync(key, callTimes + 1, this.Repository.DefaultTtl);
            }
        }

        private async Task<List<SagaContextCollectionMetric>> GetSagaContextCollectionMetricsAsync(Guid correlationId)
        {
            var iRepositoryType = typeof(IFlowSagaContextRepository<>);

            var result = new List<SagaContextCollectionMetric>();
            var repositoryProperties = FlowSagaContextUtil.GetRepositoryProperties<TSagaContext>();

            foreach (var propertyInfo in repositoryProperties)
            {
                var propertyKey = FlowSagaContextUtil.CreateSagaContextPropertyKey(correlationId, propertyInfo.Name);

                if (propertyInfo.PropertyType.GetGenericTypeDefinition() == iRepositoryType)
                {
                    var propertySize = await this.Repository.GetListLengthAsync(propertyKey);
                    result.Add(new SagaContextCollectionMetric(propertyInfo.Name, propertySize));
                }
                else
                {
                    var propertyValue = await this.Repository.GetAsync(propertyKey);
                    if (propertyValue != null)
                    {
                        var collections = GetCollectionProperties(propertyValue);
                        foreach (var collection in collections)
                        {
                            result.Add(new SagaContextCollectionMetric($"{propertyInfo.Name}_{collection.Key}", collection.Value.Count));
                        }
                    }
                }
            }

            return result.Where(m => m.CollectionSize > 0).ToList();
        }

        private static Dictionary<string, ICollection> GetCollectionProperties(object obj)
        {
            var type = obj.GetType();
            var result = new Dictionary<string, ICollection>();
            foreach (var prop in type.GetProperties())
            {
                if (typeof(ICollection).IsAssignableFrom(prop.PropertyType))
                {
                    var get = prop.GetGetMethod();
                    if (!get.IsStatic && get.GetParameters().Length == 0) // skip indexed & static
                    {
                        var collection = (ICollection)get.Invoke(obj, null);
                        if (collection != null)
                            result.Add(prop.Name, collection);
                    }
                }
            }
            return result;
        }

        protected async Task SendResponseAsync<TResponseData>(ConsumeContext<FlowRequest<TRequest>> ctx, TResponseData data)
        {
            if (ctx.Message.DoNotSendResponse) return;

            var response = new FlowResponse();

            if (data != null)
                response.JsonResult = JsonConvert.SerializeObject(data);

            await SendResponseAsync(ctx, response);
        }

        protected async Task SendResponseAsync(ConsumeContext<FlowRequest<TRequest>> ctx)
        {
            if (ctx.Message.DoNotSendResponse) return;

            var response = new FlowResponse();
            await SendResponseAsync(ctx, response);
        }

        private static async Task SendResponseAsync(ConsumeContext<FlowRequest<TRequest>> ctx, FlowResponse response)
        {
            response.CorrelationId = ctx.Message.CorrelationId;
            response.FlowTaskId = ctx.Message.FlowTaskId;
            response.FlowTaskChunkIndex = ctx.Message.FlowTaskChunkIndex;

            var endpoint = await ctx.GetSendEndpoint(ctx.SourceAddress);

            await endpoint.Send(response);
        }
    }
}
