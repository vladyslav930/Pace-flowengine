using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Consumers
{
    public class ConsumerSettings
    {
        public bool DisableCheckForRepeatedCalls { get; set; }

        public int MaxCallsIfIdempotent { get; set; } = 3;

        public TimeSpan WaitBeforeRaiseDuplicateConsumerException { get; set; } = new TimeSpan(0, 3, 0);
    }
}
