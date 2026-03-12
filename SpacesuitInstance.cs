public class SpacesuitInstance
{
    public SpacesuitData data;
    public float currentDurability;

    public SpacesuitInstance(SpacesuitData data)
    {
        this.data = data;
        this.currentDurability = data.maxDurability;
    }

    public bool IsBroken => currentDurability <= 0f;
    public float Ratio => currentDurability / data.maxDurability; // 0〜1（UI用）

    public float ApplyDamage(float damage)
    {
        // 耐久が残っていればダメージ軽減
        if (!IsBroken)
        {
            float reduced = damage * (1f - data.damageReduction);
            currentDurability -= damage * data.damageReduction;
            currentDurability = UnityEngine.Mathf.Clamp(currentDurability, 0f, data.maxDurability);
            return reduced;
        }
        return damage;
    }
}
