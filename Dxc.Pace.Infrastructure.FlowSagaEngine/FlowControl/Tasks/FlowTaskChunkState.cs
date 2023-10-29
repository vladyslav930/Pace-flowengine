namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTaskChunkState
    {
        internal FlowTaskChunkState() { }
        public int Index { get; set; }
        public FlowTaskState State { get; set; }
        public FlowTaskStateException ChunkException { get; set; }
        public override int GetHashCode()
        {
            return Index.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is FlowTaskChunkState other
                && Index.Equals(other.Index);
        }
    }

}
