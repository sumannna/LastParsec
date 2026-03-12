public class ThrusterTankInstance
{
    public ThrusterTankData data;
    public float currentFuel;

    public ThrusterTankInstance(ThrusterTankData data)
    {
        this.data = data;
        this.currentFuel = data.maxTankFuel;
    }

    public bool HasFuel => currentFuel > 0f;
    public float Ratio => currentFuel / data.maxTankFuel; // 0〜1（UI用）
}
