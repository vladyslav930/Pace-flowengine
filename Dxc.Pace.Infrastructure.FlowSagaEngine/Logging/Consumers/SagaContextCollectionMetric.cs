namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers
{    
    public class SagaContextCollectionMetric
    {
        public string CollectionName { get; set; }

        public long CollectionSize { get; set; }

        public SagaContextCollectionMetric(string collectionName, long collectionSize)
        {
            CollectionName = collectionName;
            CollectionSize = collectionSize;
        }
    }
}