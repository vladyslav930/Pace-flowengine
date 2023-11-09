using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Settings
{
    public class FlowEngineSettings
    {
        static readonly TimeSpan defaultRetryInterval = TimeSpan.FromSeconds(30);
        static readonly TimeSpan defaultRestartInterval = TimeSpan.FromSeconds(5);
        const int defaultRetryCount = 3;

        public FlowLogLevel LogLevel { get; set; } = FlowLogLevel.Basic;
        public bool ShouldRetry { get; set; } = false;
        public int RetryCount { get; set; } = defaultRetryCount;
        public TimeSpan RetryInterval { get; set; } = defaultRetryInterval;
        public string RedisConnectionString { get; set; }
        public string SqlConnectionString { get; set; }
        public bool UseRedis { get; set; }
        public bool UseInMemoryOutbox { get; set; }
        public TimeSpan SagaRestartDelay { get; set; } = defaultRestartInterval;
    }
}
