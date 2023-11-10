using Dxc.Pace.Infrastructure.FlowSagaEngine.SagaContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Utils
{
    public static class FlowSagaContextUtil
    {
        public async static Task ClearSagaContextAsync<TSagaContext>(Guid correlationId, IFlowSagaContextRepository repository)
            where TSagaContext : class
        {
            var propertiesToClear = GetRepositoryProperties<TSagaContext>()
                .Where(p => !p.GetCustomAttributes(typeof(IgnoreOnSagaFinalizationAttribute), inherit: true).Any())
                .ToList();

            if (!propertiesToClear.Any()) return;

            var keys = propertiesToClear.Select(x => CreateSagaContextPropertyKey(correlationId, x.Name));

            await repository.RemoveAllAsync(keys);
        }

        public static TSagaContext CreateSagaContext<TSagaContext>(Guid correlationId, IFlowSagaContextRepository repository)
            where TSagaContext : class
        {
            var repositoryProperties = GetRepositoryProperties<TSagaContext>();

            var context = DynamicActivator.CreateInstance<TSagaContext>();

            var flowRepositoryType = typeof(FlowSagaContextRepository<>);
            foreach (var property in repositoryProperties)
            {
                var key = CreateSagaContextPropertyKey(correlationId, property.Name);

                var tArgs = property.PropertyType.GetGenericArguments();
                var specificRepoType = flowRepositoryType.MakeGenericType(tArgs);
                var repoInstance = Activator.CreateInstance(specificRepoType, key, repository);
                property.SetValue(context, repoInstance);
            }

            return context;
        }

        public static string CreateSagaContextPropertyKey(Guid correlationId, string propertyName)
        {
            return $"{correlationId}_{propertyName}";
        }

        internal static IEnumerable<PropertyInfo> GetRepositoryProperties<TSagaContext>() where TSagaContext : class
        {
            var iRepositoryType = typeof(IFlowSagaContextRepository<>);
            var iValueType = typeof(IFlowSagaContextValue<>);

            var repositoryProperties = GetPublicProperties(typeof(TSagaContext))
                .Where(p => p.PropertyType.IsGenericType
                        && (p.PropertyType.GetGenericTypeDefinition() == iRepositoryType
                            || p.PropertyType.GetGenericTypeDefinition() == iValueType))
                .ToList();

            return repositoryProperties;
        }

        private static IEnumerable<PropertyInfo> GetPublicProperties(this Type type)
        {
            if (!type.IsInterface)
                return type.GetProperties();

            return (new Type[] { type })
                   .Concat(type.GetInterfaces())
                   .SelectMany(i => i.GetProperties(BindingFlags.Public | BindingFlags.Instance));
        }
    }
}
