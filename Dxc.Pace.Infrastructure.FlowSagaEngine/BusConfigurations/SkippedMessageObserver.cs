using System;
using MassTransit;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.BusConfigurations
{
    public class SkippedMessageObserver : IReceiveObserver
    {
        const string mtReasonHeaderName = "MT-Reason";
        const string deadLetterReasonValue = "dead-letter";

        public Task PreReceive(ReceiveContext context)
        {
            return context.ReceiveCompleted;
        }

        public async Task PostReceive(ReceiveContext context)
        {
            string mtReason = context.TransportHeaders.Get<string>(mtReasonHeaderName);
            var isDeadLetter = deadLetterReasonValue.Equals(mtReason, StringComparison.OrdinalIgnoreCase);

            if (isDeadLetter)
            {
                await SendMissingConsumerResponseAsync(context);
            }

            await context.ReceiveCompleted;
        }

        private static async Task SendMissingConsumerResponseAsync(ReceiveContext context)
        {
            var bodyBytes = context.GetBody();
            var bodyJson = Encoding.UTF8.GetString(bodyBytes, 0, bodyBytes.Length);

            var message = JsonConvert.DeserializeObject<ReceivedMtMessage>(bodyJson);

            var request = message.Message;
            var missingConsumerTypeName = message.GetMissingConsumerTypeName();
            var exceptionMessage = $"Failed to find consumer of type {missingConsumerTypeName}";

            var response = new FailedFlowResponse
            {
                CorrelationId = request.CorrelationId,
                FlowTaskId = request.FlowTaskId,
                FlowTaskChunkIndex = request.FlowTaskChunkIndex,
                ExceptionMessage = exceptionMessage
            };

            var sagaAddress = new Uri(message.FaultAddress);
            var endpoint = await context.SendEndpointProvider.GetSendEndpoint(sagaAddress);
            await endpoint.Send(response);
        }

        public Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType)
            where T : class
        {
            return context.ConsumeCompleted;
        }

        public Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan elapsed, string consumerType, Exception exception) where T : class
        {
            return context.ConsumeCompleted;
        }

        public Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            return context.ReceiveCompleted;
        }
    }
}