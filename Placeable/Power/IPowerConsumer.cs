public interface IPowerConsumer
{
    string ConsumerName { get; }
    float PowerConsumption { get; }
    bool IsRunning { get; }
    bool IsOn { get; }
    ElectricConnector Connector { get; }
    void OnPowerSupplied();
    void OnPowerCutOff();
}