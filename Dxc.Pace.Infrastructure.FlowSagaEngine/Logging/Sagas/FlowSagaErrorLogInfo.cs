using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas
{
    public class FlowSagaErrorLogInfo : FlowSagaLogInfo
    {
        public Exception Exception { get; set; }
    }
}
