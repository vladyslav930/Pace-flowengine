using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers
{
    public class ExceptionInfo
    {
        private const int MaxMessageLength = 500;

        public string Message { get; private set; }
        public string StackTrace { get; private set; }
        public ExceptionInfo InnerExceptionInfo { get; private set; }

        private ExceptionInfo()
        {
        }

        public static ExceptionInfo Create(Exception exception)
        {
            var exceptionInfo = new ExceptionInfo
            {
                Message = exception.Message.Substring(0, MaxMessageLength < exception.Message.Length ? MaxMessageLength : exception.Message.Length),
                StackTrace = exception.StackTrace
            };
            if (exception.InnerException != null)
            {
                exceptionInfo.InnerExceptionInfo = Create(exception.InnerException);
            }

            return exceptionInfo;
        }

        public override string ToString()
        {
            return $"{{Exception: {Message}, StackTrace: {StackTrace}{GetInnerExceptionString(InnerExceptionInfo)}}}";
        }

        private string GetInnerExceptionString(ExceptionInfo innerExceptionInfo)
        {
            return innerExceptionInfo == null ? string.Empty : $", InnerException: {innerExceptionInfo}";
        }
    }
}
