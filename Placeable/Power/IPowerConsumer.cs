public interface IPowerConsumer
{
    string ConsumerName { get; }
    float PowerConsumption { get; }
    bool IsRunning { get; }
    bool IsOn { get; }
    /// <summary>ON‚©‚ÂÀÛ‚Éˆ—’†i“d—ÍÁ”ï‚·‚×‚«ó‘Ôj‚©‚Ç‚¤‚©</summary>
    bool IsConsuming { get; }
    ElectricConnector Connector { get; }
    void OnPowerSupplied();
    void OnPowerCutOff();
}