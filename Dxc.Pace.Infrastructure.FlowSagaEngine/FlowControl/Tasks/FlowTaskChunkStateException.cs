using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks
{
    public class FlowTaskStateException
    {
        public Exception Exception { get; set; }
        public bool IsLogged { get; set; }
    }

}
