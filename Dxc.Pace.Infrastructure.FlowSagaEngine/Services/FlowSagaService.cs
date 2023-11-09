using Dxc.Pace.Infrastructure.FlowSagaEngine.Utils;
using MassTransit;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.Core.Configuration.LogManager;
using Dxc.Pace.Infrastructure.Core.Jwt.Extensions;
using Dxc.Pace.Infrastructure.FlowSagaEngine.QueueSagas;
using Dxc.Pace.Infrastructure.MassTransit.Logging.Helpers;
using Dxc.Pace.Infrastructure.MassTransit.Settings.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Dxc.Pace.Infrastructure.Core.Utils;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Services
{
    internal class FlowSagaService : IFlowSagaService
    {
        private const string flowSagaCorrelationIdHeaderName = "Flow-Saga-CorrelationId";

        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IBusControl bus;
        private readonly IFlowSagaContextRepository repository;
        private readonly RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider;
        private readonly ICorrelationLogManager correlationLogManager;
        private readonly IConfiguration configuration;
        private readonly bool shouldUseCostingFlowsQueue;


        public FlowSagaService(
            IHttpContextAccessor httpContextAccessor,
            IBusControl bus,
            IFlowSagaContextRepository repository,
            RabbitMqConnectionStringProvider rabbitMqConnectionStringProvider,
            ICorrelationLogManager correlationLogManager,
            IConfiguration configuration)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.bus = bus;
            this.repository = repository;
            this.rabbitMqConnectionStringProvider = rabbitMqConnectionStringProvider;
            this.correlationLogManager = correlationLogManager;
            this.configuration = configuration;

            this.shouldUseCostingFlowsQueue = this.configuration.GetValue(QueueSagasConstants.SagaSettingsUseCostingFlowsQueue, true);
        }

        public Task StartSagaAsync<TFlowData>(TFlowData data)
            where TFlowData : class, ILaunchSettings, new()
        {
            return StartSagaAsync<TFlowData, object>(data, null);
        }

        public async Task StartSagaAsync<TFlowData, TSagaContext>(
            TFlowData data, Func<TSagaContext, Task> contextProcessor = null, string emailPrincipal = null)
            where TFlowData : class, ILaunchSettings, new()
            where TSagaContext : class
        {
            var rabitMqConnectionString = this.rabbitMqConnectionStringProvider.ConnectionString;
            var queueAddress = FlowQueueUtil.GetFlowSagaQueueAddress<TFlowData>(rabitMqConnectionString);
            var faultAddress = FlowQueueUtil.GetUnifiedFaultAddress(rabitMqConnectionString);

            var endpoint = await bus.GetSendEndpoint(queueAddress);

            Guid? userId = null;
            string userEmail;
            if (string.IsNullOrEmpty(emailPrincipal))
            {
                userId = httpContextAccessor.HttpContext?.User.GetId();
                userEmail = httpContextAccessor.HttpContext?.User.GetEmail();
            }
            else
            {
                userEmail = emailPrincipal;
            }

            var command = new FlowSagaStartCommand<TFlowData>(userId, userEmail)
            {
                FlowData = data
            };

            if (contextProcessor != null)
            {
                var correlationId = command.CorrelationId;
                var context = FlowSagaContextUtil.CreateSagaContext<TSagaContext>(correlationId, repository);
                await contextProcessor(context);
            }

            AddFlowSagaCorrelationHeaderToResponse(command.CorrelationId);

            if (data.ShouldUseDeferredQueue && this.shouldUseCostingFlowsQueue)  // we support queue for only costing requests now
            {
                var startCommandType = typeof(FlowSagaStartCommand<TFlowData>);
                var costingFlowsQueueRequest = new CostingFlowsQueueRequest()
                {
                    QueueId = command.FlowData.QueueId,
                    CostingVersionId = IntToGuidConverter.GuidToInt(command.FlowData.QueueId),
                    CorrelationId = command.CorrelationId,
                    TargetLaunchCommand = command,
                    TargetLaunchCommandType = $"{startCommandType.FullName}, {startCommandType.Assembly.FullName}",
                };
                var costingFlowsQueueAddress = FlowQueueUtil.GetQueueAddress(rabitMqConnectionString, QueueSagasConstants.CostingFlowsQueueName);
                var costingFlowsEndpoint = await bus.GetSendEndpoint(costingFlowsQueueAddress);
                await CorrelationUtil.SendWithCorrelationHeadersAsync(costingFlowsEndpoint, (CorrelationLogManager)correlationLogManager, costingFlowsQueueRequest, faultAddress);
            }
            else
            {
                await CorrelationUtil.SendWithCorrelationHeadersAsync(endpoint, (CorrelationLogManager)correlationLogManager, command, faultAddress);
            }
        }

        public Task StartSagaAsync<TFlowData, TSagaContext>(TFlowData data, Action<TSagaContext> contextProcessor = null)
            where TFlowData : class, ILaunchSettings, new()
            where TSagaContext : class
        {
            if (contextProcessor == null) return StartSagaAsync<TFlowData, TSagaContext>(data, null);

            Task asyncFactory(TSagaContext c)
            {
                contextProcessor(c);
                return Task.CompletedTask;
            }

            return StartSagaAsync(data, (Func<TSagaContext, Task>)asyncFactory);
        }

        private void AddFlowSagaCorrelationHeaderToResponse(Guid correlationId)
        {
            if (httpContextAccessor.HttpContext != null)
            {
                var headers = httpContextAccessor.HttpContext.Response.Headers;
                var correlationString = correlationId.ToString();
                if (headers.ContainsKey(flowSagaCorrelationIdHeaderName))
                {
                    var value = headers[flowSagaCorrelationIdHeaderName];
                    headers[flowSagaCorrelationIdHeaderName] = new StringValues(
                        value.ToArray()
                            .Union(new[] { correlationString })
                            .ToArray());
                }
                else
                {
                    headers.Add(flowSagaCorrelationIdHeaderName, correlationString);
                }
            }
        }
    }
}
