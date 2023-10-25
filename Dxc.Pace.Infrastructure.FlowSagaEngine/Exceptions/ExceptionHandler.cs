using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Exceptions
{
    public delegate bool ExceptionHandler<TException>(TException exception) where TException : Exception;
}
