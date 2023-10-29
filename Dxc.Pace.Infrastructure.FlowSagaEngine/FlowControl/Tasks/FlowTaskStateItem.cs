namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTaskStateItem
    {
        internal FlowTaskStateItem() { }
        public int TaskId { get; set; }
        public FlowTaskStateContainer StatesContainer { get; set; } = new FlowTaskStateContainer();
        public override int GetHashCode()
        {
            return TaskId.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is FlowTaskStateItem other
                && TaskId.Equals(other.TaskId);
        }
    }

}
