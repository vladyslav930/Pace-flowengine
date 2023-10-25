using Dxc.Pace.Infrastructure.FlowSagaEngine.Settings;
using GreenPipes.Configurators;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Exceptions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public abstract class FlowExceptionPolicyBaseAttribute : Attribute
    {
        protected FlowEngineSettings Settings;
        public bool ShouldUseRetry { get; set; } = true;

        public void RetryConfigurator(IRetryConfigurator retryConfigurator)
        {
            ConfigureExceptionHandlers(retryConfigurator);
            ConfigureInterval(retryConfigurator);
        }

        public virtual void Initialize(IServiceProvider serviceProvider)
        {
            this.Settings = serviceProvider.GetRequiredService<FlowEngineSettingsStorage>().FlowSagaEngineSettings;
        }

        protected abstract List<IFlowExceptionConfiguration> GetFlowExceptionConfigurations();
        protected abstract void ConfigureInterval(IRetryConfigurator retryConfigurator);
        private void ConfigureExceptionHandlers(IRetryConfigurator retryConfigurator)
        {
            var configurations = GetFlowExceptionConfigurations();
            if (!configurations.Any()) return;

            retryConfigurator.Handle<Exception>(rootException =>
            {
                var exceptions = GetSelfAndInnerExceptions(rootException);
                foreach (var configuration in configurations)
                {
                    var configurationExceptionFilter = configuration.ExceptionTypeFullNameFilter;
                    var configurationException = exceptions
                        .FirstOrDefault(x => x.GetType().FullName.Contains(configurationExceptionFilter));

                    if (configurationException == null) 
                        continue;

                    var shouldRetry = this.Settings.ShouldRetry 
                        && configuration.ShouldRetryHandler(configurationException);
                    if (shouldRetry) 
                        return true;
                }

                return false;
            });
        }

        private static List<Exception> GetSelfAndInnerExceptions(Exception exception)
        {
            var exceptions = new List<Exception>() { exception };

            if (exception.InnerException != null)
            {
                var innerExceptions = GetSelfAndInnerExceptions(exception.InnerException);
                exceptions.AddRange(innerExceptions);
            }

            return exceptions;
        }
    }
}
