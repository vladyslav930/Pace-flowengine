using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTaskStateContainer
    {
        internal FlowTaskStateContainer() { }
        public FlowTaskChunkState this[int i] => ChunkStates.Single(x => x.Index == i);
        public List<FlowTaskChunkState> ChunkStates { get; set; }
        public FlowTaskStateException TaskException { get; set; }

        public bool CheckAll(params FlowTaskState[] statesToCheck)
        {
            return ChunkStates.All(x => statesToCheck.Contains(x.State));
        }

        public bool CheckAny(params FlowTaskState[] statesToCheck)
        {
            return ChunkStates.Any(x => statesToCheck.Contains(x.State));
        }
        public void SetAll(FlowTaskState state)
        {
            foreach (var item in ChunkStates)
                item.State = state;
        }

        public List<FlowTaskStateException> GetExceptions()
        {
            var exceptions = ChunkStates
                .Select(x => x.ChunkException)
                .ToList();

            exceptions.Add(TaskException);

            exceptions = exceptions
                .Where(x => x != null)
                .ToList();

            return exceptions;
        }
    }
}
