public interface IBatterySource
{
    string SourceName { get; }
    float MaxCapacity { get; }
    float CurrentCharge { get; }
    float ChargeRatio { get; }
    ElectricConnector Connector { get; }
    void Charge(float kWh);
    float Discharge(float kWh);
}