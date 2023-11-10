using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using ImpromptuInterface;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Utils

{
    public static class DynamicActivator
    {
        public static TObject CreateInstance<TObject>()
            where TObject : class
        {
            return CreateInstanceAndPopulateProperties<TObject>(null);
        }

        public static TObject CreateInstanceAndPopulateProperties<TObject>(Dictionary<string, object> propertiesValues)
            where TObject : class
        {
            var desiredType = typeof(TObject);

            if (desiredType.IsInterface)
            {
                return CreateInterfaceInstance<TObject>(propertiesValues);
            }
            else
            {
                var objectCreated = Activator.CreateInstance<TObject>();
                PopulatePropertiesOfObjectWithValuesFromDictionary(objectCreated, propertiesValues);
                return objectCreated;
            }
        }

        private static TInterface CreateInterfaceInstance<TInterface>(Dictionary<string, object> propertiesValues)
            where TInterface : class
        {
            dynamic expandoObject = new ExpandoObject();

            var expandoType = typeof(TInterface);
            var expandoProperties = expandoObject as IDictionary<String, object>;

            foreach (var property in GetPublicProperties(expandoType))
            {
                if (propertiesValues != null && propertiesValues.ContainsKey(property.Name))
                {
                    var parameterValue = propertiesValues[property.Name];
                    var convertedValue = CastValueToPropertyType(property, parameterValue);
                    expandoProperties[property.Name] = convertedValue;
                }
                else
                {
                    var propertyType = property.PropertyType;
                    object defaultValue = propertyType.IsValueType ? Activator.CreateInstance(propertyType) : null;
                    expandoProperties[property.Name] = defaultValue;
                }
            }

            return Impromptu.ActLike<TInterface>(expandoObject);
        }

        private static void PopulatePropertiesOfObjectWithValuesFromDictionary(object obj, Dictionary<string, object> propertiesValues)
        {
            if (propertiesValues == null) return;

            var objectType = obj.GetType();

            foreach (var property in GetPublicProperties(objectType))
            {
                if (propertiesValues.ContainsKey(property.Name))
                {
                    var parameterValue = propertiesValues[property.Name];
                    var convertedValue = CastValueToPropertyType(property, parameterValue);
                    property.SetValue(obj, convertedValue);
                }
            }
        }

        private static object CastValueToPropertyType(PropertyInfo propertyToBeSetted, object value)
        {
            Type propertyType = Nullable.GetUnderlyingType(propertyToBeSetted.PropertyType) ?? propertyToBeSetted.PropertyType;
            object safeValue = (value == null) ? null : Convert.ChangeType(value, propertyType);

            return safeValue;
        }

        private static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }
    }
}