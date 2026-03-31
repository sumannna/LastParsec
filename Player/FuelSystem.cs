using UnityEngine;

public class FuelSystem : MonoBehaviour
{
    [Header("参照")]
    public StatusBarManager statusBarManager;

    public ThrusterTankInstance equippedTank = null;

    public void UseFuel(float deltaTime)
    {
        if (equippedTank == null) return;

        equippedTank.currentFuel -= 10f * deltaTime;
        equippedTank.currentFuel = Mathf.Clamp(equippedTank.currentFuel, 0f,
            equippedTank.data.maxTankFuel);

        if (statusBarManager != null)
            statusBarManager.SetValue("thruster", equippedTank.Ratio);
    }

    public bool HasFuel()
    {
        return equippedTank != null && equippedTank.HasFuel;
    }

    public void AddFuel(float amount)
    {
        if (equippedTank == null) return;
        equippedTank.currentFuel = Mathf.Clamp(
            equippedTank.currentFuel + amount, 0f, equippedTank.data.maxTankFuel);
    }
}
