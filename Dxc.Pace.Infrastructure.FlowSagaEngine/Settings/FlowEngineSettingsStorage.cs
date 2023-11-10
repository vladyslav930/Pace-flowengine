namespace Dxc.Pace.Infrastructure.FlowSagaEngine.Settings
{
    public class FlowEngineSettingsStorage
    {
        public FlowEngineSettings FlowSagaEngineSettings { get; }

        public FlowEngineSettingsStorage(FlowEngineSettings flowSagaEngineSettings)
        {
            FlowSagaEngineSettings = flowSagaEngineSettings;
        }
    }
}
