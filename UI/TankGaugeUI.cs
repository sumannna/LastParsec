using UnityEngine;

public class TankGaugeUI : MonoBehaviour
{
    [Header("参照")]
    public OxygenSystem oxygenSystem;
    public StatusBarManager statusBarManager;

    void Update()
    {
        if (oxygenSystem == null || statusBarManager == null) return;

        OxygenTankInstance tank = oxygenSystem.equippedTank;

        if (tank != null)
        {
            statusBarManager.SetVisible("oxygenTank", true);
            statusBarManager.SetValue("oxygenTank", tank.Ratio);
        }
        else
        {
            statusBarManager.SetVisible("oxygenTank", false);
        }
    }
}
