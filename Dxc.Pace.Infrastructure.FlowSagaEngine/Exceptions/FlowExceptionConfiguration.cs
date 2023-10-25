using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Exceptions
{
    public class FlowExceptionConfiguration<TException> : IFlowExceptionConfiguration
        where TException : Exception
    {
        public FlowExceptionConfiguration() { }

        public ExceptionHandler<TException> ShouldRetry { get; set; } = e => true;
        public ExceptionHandler<Exception> ShouldRetryHandler => e => ShouldRetry(e as TException);
        public string ExceptionTypeFullNameFilter { get; set; } = typeof(TException).FullName;
    }
}
