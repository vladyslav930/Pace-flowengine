using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Consumers;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Logging
{
    public class ConsoleFlowLogger : IConsoleFlowLogger
    {
        private readonly IConsoleFlowLoggerWriter consoleFlowLoggerWriter;

        public ConsoleFlowLogger(IConsoleFlowLoggerWriter consoleFlowLoggerWriter)
        {
            this.consoleFlowLoggerWriter = consoleFlowLoggerWriter;
        }

        public Task LogFlowConsumer<TRequest>(FlowConsumerLogInfo<TRequest> info) where TRequest : class
        {
            var time = GetTime();
            var requestTypeName = info.Request.GetType().GenericTypeArguments[0].Name;
            var eventTypeName = Enum.GetName(typeof(FlowConsumerEventType), info.EventType);
            if (info.EventType == FlowConsumerEventType.ConsumerError)
            {
                var infoStr = JsonConvert.SerializeObject(info);
                this.consoleFlowLoggerWriter.Error($"--LogFlowConsumer Error\t{time}\ttype: {eventTypeName}\tRequestType: {requestTypeName}\t{infoStr}");
            }
            else
            {
                this.consoleFlowLoggerWriter.Log($"--LogFlowConsumer\t{time}\ttype: {eventTypeName}\tFlowTaskId: {info.Request.FlowTaskId}\tRequestType: {requestTypeName}", ConsoleColor.DarkYellow);
            }

            return Task.CompletedTask;
        }

        public Task LogFlowSaga(FlowSagaLogInfo info)
        {
            var time = GetTime();
            var eventTypeName = Enum.GetName(typeof(FlowSagaEventType), info.EventType);
            if (info.EventType == FlowSagaEventType.SagaFinallyFlowError
                || info.EventType == FlowSagaEventType.SagaMainFlowError)
            {
                var infoStr = JsonConvert.SerializeObject(info);
                this.consoleFlowLoggerWriter.Error($"--LogFlowSaga Error\t{time}\ttype: {eventTypeName}\t{infoStr}");
            }
            else
            {
            	var line = $"--LogFlowSaga\t\t{time}\ttype: {eventTypeName}\tSagaClassFullName: {info.SagaClassFullName}\tCorrelationId: {info.CorrelationId}";                    
                if (!string.IsNullOrEmpty(info.Message))
	            {
	                line += $"\tMessage: {info.Message}";
	            }
                this.consoleFlowLoggerWriter.Log(line, ConsoleColor.DarkGreen);
            }

            return Task.CompletedTask;
        }

        public Task LogFlowTask<TFlowData>(FlowTaskLogInfo<TFlowData> info) where TFlowData : class, new()
        {
            var time = GetTime();
            var eventTypeName = Enum.GetName(typeof(FlowTaskEventType), info.EventType);
            this.consoleFlowLoggerWriter.Log(
                $"--LogFlowTask\t\t{time}\ttype: {eventTypeName}\tTaskId: {info.TaskId}\tTaskName: {info.TaskName}\tSagaClassFullName: {info.SagaClassFullName}",
                ConsoleColor.DarkYellow);

            return Task.CompletedTask;
        }

        public Task LogFlowRequest<TData>(FlowRequestLogInfo<TData> info) where TData : class
        {
            var time = GetTime();
            var eventTypeName = Enum.GetName(typeof(FlowRequestEventType), info.EventType);
            this.consoleFlowLoggerWriter.Log(
                $"--LogFlowRequest\t{time}\ttype: {eventTypeName}\tTaskId: {info.TaskId}\tTaskName: {info.TaskName}\tSagaClassFullName: {info.SagaClassFullName}",
                ConsoleColor.DarkMagenta);

            return Task.CompletedTask;
        }

        public Task LogData<TData>(TData customData, Guid correlationId)
        {
            Console.WriteLine($"--CustomData: {customData}");
            return Task.CompletedTask;
        }

        private static string GetTime()
        {
            var time = DateTime.Now.ToString("HH:mm:ss.fff");
            return time;
        }
    }
}
