using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Services
{
    public interface ILaunchSettings
    {
        bool ShouldUseDeferredQueue { get; }

        Guid QueueId { get; }
    }
}
