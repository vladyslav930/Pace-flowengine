using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public class ConsoleFlowLoggerWriter : IConsoleFlowLoggerWriter
    {
        private static readonly object locker = new object();

        public void Error(string message)
        {
            this.Log(message, ConsoleColor.DarkRed);
        }

        public void Log(string message, ConsoleColor color)
        {
            lock (locker)
            {
                var previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ForegroundColor = previousColor;
            }
        }
    }
}
