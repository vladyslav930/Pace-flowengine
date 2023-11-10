using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Utils
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class IgnoreOnSagaFinalizationAttribute : Attribute
    {
    }
}
