public interface IPowerConsumer
{
    string ConsumerName { get; }
    float PowerConsumption { get; } // kW
    bool IsRunning { get; }
    void OnPowerSupplied();   // “d—Í‹Ÿ‹‹Žž‚ÉŒÄ‚Î‚ê‚é
    void OnPowerCutOff();     // “d—Í•s‘«Žž‚ÉŒÄ‚Î‚ê‚é
}