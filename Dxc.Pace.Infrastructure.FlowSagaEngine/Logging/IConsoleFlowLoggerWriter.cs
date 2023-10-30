using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public interface IConsoleFlowLoggerWriter
    {
        void Log(string message, ConsoleColor color);

        void Error(string message);
    }
}
