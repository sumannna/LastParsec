public interface IBatterySource
{
    string SourceName { get; }
    float MaxCapacity { get; }    // kWh
    float CurrentCharge { get; } // kWh
    float ChargeRatio { get; }   // 0~1
    void Charge(float kWh);
    float Discharge(float kWh);  // ÀÛ‚É•ú“d‚Å‚«‚½—Ê‚ğ•Ô‚·
}