public class OxygenTankInstance
{
    public OxygenTankData data;
    public float currentOxygen;

    public OxygenTankInstance(OxygenTankData data)
    {
        this.data = data;
        this.currentOxygen = data.maxTankOxygen;
    }

    public bool HasOxygen => currentOxygen > 0f;
    public float Ratio => currentOxygen / data.maxTankOxygen; // 0〜1（UI用）
}
