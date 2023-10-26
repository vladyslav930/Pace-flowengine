using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Instances;
using Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Tasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.FlowTasks;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Requests;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Logging.Sagas;
using Dxc.Pace.Infrastructure.FlowSagaEngine.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas
{
    internal class FlowSagaLogger<TFlowData, TSagaContext>
        where TFlowData : class, new()
        where TSagaContext : class
    {
        private readonly IFlowSagaLogger logger;
        private readonly FlowLogLevel logLevel;
        private readonly string sagaClassFullName;

        public FlowSagaLogger(IFlowSagaLogger logger, Type sagaType, FlowLogLevel logLevel)
        {
            this.logger = logger;
            this.logLevel = logLevel;
            sagaClassFullName = sagaType.FullName;
        }

        internal Task LogFlowSaga(
            FlowSagaEventType eventType,
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            string message = null)
        {
            var info = new FlowSagaLogInfo()
            {
                EventType = eventType,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                SagaClassFullName = sagaClassFullName,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                Message = message
            };
            return logger.LogFlowSaga(info);
        }

        public async Task LogFlowSagaError(
            FlowContainer<TFlowData, TSagaContext> flowContainer,
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            Exception additionalException = null)
        {
            var mainFlowExceptions = flowContainer
                .GetMainFlowExceptions()
                .Where(x => !x.IsLogged)
                .ToList();
            if (mainFlowExceptions.Any())
            {
                var exceptions = mainFlowExceptions.Select(x => x.Exception);
                await LogFlowSagaError(FlowSagaEventType.SagaMainFlowError, flowInstance, exceptions);
                mainFlowExceptions.ForEach(e => e.IsLogged = true);
            }

            var finallyFlowExceptions = flowContainer
                .GetFinallyFlowExceptions()
                .Where(x => !x.IsLogged)
                .ToList();
            if (finallyFlowExceptions.Any())
            {
                var exceptions = finallyFlowExceptions.Select(x => x.Exception);
                await LogFlowSagaError(FlowSagaEventType.SagaFinallyFlowError, flowInstance, exceptions);
                finallyFlowExceptions.ForEach(e => e.IsLogged = true);
            }

            if (additionalException != null)
            {
                await LogFlowSagaError(FlowSagaEventType.SagaFinallyFlowError, flowInstance, new[] { additionalException });
            }

            flowInstance.UpdateTasksStates();
        }

        public Task LogFlowSagaError(
                    FlowSagaEventType eventType,
                    FlowInstance<TFlowData, TSagaContext> flowInstance,
                    IEnumerable<Exception> exceptions)
        {
            Exception exception;
            if (exceptions.Count() > 1)
            {
                var errorMessage = JsonConvert.SerializeObject(exceptions.ToList());
                exception = new Exception(errorMessage);
            }
            else
            {
                exception = exceptions.First();
            }

            var info = new FlowSagaErrorLogInfo()
            {
                EventType = eventType,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                SagaClassFullName = sagaClassFullName,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                Exception = exception,
            };
            return logger.LogFlowSaga(info);
        }

        public Task LogFlowTask(
            FlowTaskEventType eventType,
            FlowTask<TFlowData, TSagaContext> task,
            FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            var shouldLog = (int)task.LogLevel <= (int)logLevel;

            if (!shouldLog) return Task.CompletedTask;

            var info = new FlowTaskLogInfo<TFlowData>()
            {
                EventType = eventType,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                SagaClassFullName = sagaClassFullName,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                TaskId = task.Id,
                TaskName = task.Name,
                CallerMethodName = task.CallerMethodName,
                FlowData = flowInstance.FlowData
            };
            return logger.LogFlowTask(info);
        }

        public Task LogFlowRequest(
            FlowRequestEventType eventType,
            FlowTask<TFlowData, TSagaContext> task,
            FlowInstance<TFlowData, TSagaContext> flowInstance)
        {
            return LogFlowRequest<object>(eventType, task, flowInstance, null);
        }

        public Task LogFlowRequest<TData>(
            FlowRequestEventType eventType,
            FlowTask<TFlowData, TSagaContext> task,
            FlowInstance<TFlowData, TSagaContext> flowInstance,
            FlowRequest<TData> flowRequest)
            where TData : class
        {
            var info = new FlowRequestLogInfo<TData>()
            {
                EventType = eventType,
                CorrelationId = flowInstance.CorrelationId,
                UserId = flowInstance.UserId,
                UserEmail = flowInstance.UserEmail,
                SagaClassFullName = sagaClassFullName,
                ParentSagaCorrelationId = flowInstance.ParentSagaCorrelationId,
                TaskId = task.Id,
                TaskName = task.Name,
                CallerMethodName = task.CallerMethodName,
                Request = flowRequest?.Data
            };
            return logger.LogFlowRequest(info);
        }
    }
}
