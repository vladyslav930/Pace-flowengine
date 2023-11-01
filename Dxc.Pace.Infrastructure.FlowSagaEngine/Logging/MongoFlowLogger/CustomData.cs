namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.MongoFlowLogger
{
    public class CustomData<T>
    {
        public T Data { get; set; }

        public CustomData(T data)
        {
            this.Data = data;
        }
    }
}
