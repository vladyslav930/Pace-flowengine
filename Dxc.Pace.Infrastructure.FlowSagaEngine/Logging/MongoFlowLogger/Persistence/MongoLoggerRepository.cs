using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using MongoDB.Driver;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger.Persistence
{
    public sealed class MongoLoggerRepository : IMongoLoggerRepository, IDisposable
    {
        private const string TimePropertyName = "Time";

        private readonly IMongoDatabase mongoDatabase;
        private readonly SemaphoreSlim semaphore;

        public MongoLoggerRepository(MongoLoggerDatabaseProvider databaseProvider)
        {
            mongoDatabase = databaseProvider.GetDatabase();

            var connectionsCount = mongoDatabase.Client.Settings.MaxConnectionPoolSize / 2;
            semaphore = new SemaphoreSlim(connectionsCount, connectionsCount);
        }

        public async Task InsertAsync<TEntity>(TEntity entity, string collectionName, params KeyValuePair<string, object>[] additionalProperties)
        {
            await this.semaphore.WaitAsync();
            try
            {
                var collection = this.mongoDatabase.GetCollection<ExpandoObject>(collectionName);

                var timeProperties = new KeyValuePair<string, object>(TimePropertyName, DateTime.UtcNow);
                var allAdditionalProperties = new[] { timeProperties }.Union(additionalProperties).ToArray();

                var expandedEntity = ExpandEntity(entity, allAdditionalProperties);

                await collection.BulkWriteAsync(new List<WriteModel<ExpandoObject>> { new InsertOneModel<ExpandoObject>(expandedEntity) });
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        private static ExpandoObject ExpandEntity<TEntity>(TEntity entity, params KeyValuePair<string, object>[] additionalProperties)
        {
            var expando = new ExpandoObject() as IDictionary<string, object>;

            foreach (var additionalProperty in additionalProperties)
            {
                expando.Add(additionalProperty.Key, additionalProperty.Value);
            }

            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(entity.GetType()))
            {
                if (property.Name == "Exception")
                {
                    expando.Add(property.Name, property.GetValue(entity)?.ToString());
                }
                else if (property.Name == nameof(FlowConsumerLogInfo<object>.CollectionMetrics))
                {
                    var metricsValue = property.GetValue(entity);
                    if (metricsValue != null)
                    {
                        var metrics = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var metric in (List<SagaContextCollectionMetric>)metricsValue)
                        {
                            metrics.Add(metric.CollectionName, metric.CollectionSize.ToString());
                        }

                        expando.Add("CollectionMetrics", metrics);
                    }
                }
                else
                {
                    expando.Add(property.Name, property.GetValue(entity));

                    if (property.Name == "Request")
                    {
                        var requestValue = property.GetValue(entity);
                        if (requestValue != null)
                        {
                            var requestType = requestValue.GetType();
                            var requestTypeName = requestType.IsGenericType && requestType.GenericTypeArguments.Length == 1
                                ? requestType.GenericTypeArguments[0].FullName
                                : requestType.FullName;

                            expando.Add(property.Name + "Type", requestTypeName);
                        }
                    }
                }                
            }

            return (ExpandoObject)expando;
        }

        public void Dispose()
        {
            this.semaphore?.Dispose();
        }
    }
}
