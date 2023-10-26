using System;

namespace Dxc.Pace.Infrastructure.FlowSagaEngine.FlowControl.Sagas
{
    /// <summary>
    /// Ignore Saga in Release mode
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TestFlowSagaAttribute : Attribute
    {
    }
}
