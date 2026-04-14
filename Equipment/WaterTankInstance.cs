public class WaterTankInstance
{
    public WaterTankData data;
    public float currentWater;

    public WaterTankInstance(WaterTankData data)
    {
        this.data = data;
        this.currentWater = data.maxWater;
    }

    public bool IsEmpty => currentWater <= 0f;
    public float Ratio => currentWater / data.maxWater;

    /// <summary>…•ª‚ğÁ”ïBÁ”ïŒã‚É‹ó‚È‚ç true ‚ğ•Ô‚·</summary>
    public bool Consume(float amount)
    {
        currentWater -= amount;
        currentWater = UnityEngine.Mathf.Clamp(currentWater, 0f, data.maxWater);
        return IsEmpty;
    }
}