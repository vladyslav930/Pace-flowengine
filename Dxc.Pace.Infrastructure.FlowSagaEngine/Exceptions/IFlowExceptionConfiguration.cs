using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Exceptions
{
    public interface IFlowExceptionConfiguration
    {
        ExceptionHandler<Exception> ShouldRetryHandler { get; }
        string ExceptionTypeFullNameFilter { get; }
    }
}
