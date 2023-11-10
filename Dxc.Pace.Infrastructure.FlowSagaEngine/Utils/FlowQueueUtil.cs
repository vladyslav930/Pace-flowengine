using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Utils
{
    public static class FlowQueueUtil
    {
        private const string sagaQueuePostfix = "_FlowSaga";
        private static readonly string[] sagaQueuePrefixes = new[] {
            "Dxc.Pace.Orchestrator.Contracts.",
            "Dxc.Pace.Orchestrator.QueueSagas.",
        };
        private static readonly ConcurrentDictionary<Type, string> requestQueueDictionary = new ConcurrentDictionary<Type, string>();

        public static readonly string UnifiedFaultQueueName = "UnifiedFaultQueue";
        public static Uri GetFlowSagaQueueAddress<TFlowData>(string rabitMqConnectionString) where TFlowData : class, new()
        {
            var queueName = GetFlowSagaQueueName<TFlowData>();
            return GetQueueAddress(rabitMqConnectionString, queueName);
        }

        public static Uri GetQueueAddress(string rabitMqConnectionString, string queueName)
        {
            var exchangeName = queueName;
            var relativeUri = $"{exchangeName}?bind=true&queue={queueName}";

            var queueAddress = new Uri(new Uri(rabitMqConnectionString), relativeUri);
            return queueAddress;
        }

        public static Uri GetUnifiedFaultAddress(string rabitMqConnectionString) 
            => GetQueueAddress(rabitMqConnectionString, UnifiedFaultQueueName);

        public static string GetFlowSagaQueueName<TFlowData>() where TFlowData : class, new()
        {
            var flowDataType = typeof(TFlowData);
            return GetFlowSagaQueueName(flowDataType);
        }

        public static string GetRequestQueueName<TFlowRequest>()
        {
            var requestType = typeof(TFlowRequest);
            var queueName = GetRequestQueueName(requestType);
            return queueName;
        }

        public static string GetRequestQueueName(Type requestType)
        {
            var queueName = requestQueueDictionary.GetOrAdd(requestType, t =>
            {
                var name = requestType.Assembly.GetName().Name + "_FlowRequests";
                return name;
            });
            return queueName;
        }

        public static Uri GetRequestAddress(Type requestType, string rabitMqConnectionString)
        {
            var queueName = GetRequestQueueName(requestType);
            return GetQueueAddress(rabitMqConnectionString, queueName);
        }

        public static string GetFlowSagaQueueName(Type flowDataType)
        {
            const string dataString = "Data";
            var fullName = flowDataType.FullName;
            if (fullName.EndsWith(dataString))
            {
                fullName = fullName.Substring(0, fullName.Length - dataString.Length);
            }

            var sagaQueuePrefix = sagaQueuePrefixes.SingleOrDefault(x => fullName.StartsWith(x));
            if (!string.IsNullOrEmpty(sagaQueuePrefix))
            {
                fullName = fullName.Substring(sagaQueuePrefix.Length);
            }

            var queueName = fullName + sagaQueuePostfix;
            return queueName;
        }

        public static string GetDbTableName<TFlowData>() where TFlowData : class, new()
        {
            var queueName = GetFlowSagaQueueName<TFlowData>();
            if (queueName.EndsWith(sagaQueuePostfix))
            {
                queueName = queueName.Substring(0, queueName.Length - sagaQueuePostfix.Length);
            }

            var tableName = $"{queueName}Instances".Replace(".", "_");
            return tableName;
        }
    }
}
