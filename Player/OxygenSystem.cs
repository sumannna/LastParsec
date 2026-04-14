using UnityEngine;
using UnityEngine.UI;

public class OxygenSystem : MonoBehaviour
{
    [Header("酸素設定")]
    public float maxOxygen = 100f;
    public float drainRate = 8f;        // タンク消費速度
    public float fastDrainRate = 25f;   // 無補給時の緊急減少速度
    public float recoveryRate = 30f;    // 空気あり時の回復速度

    [Header("UI")]
    public Slider oxygenBar;

    [Header("参照")]
    public CrewSystem crewSystem;
    public InventoryUI inventoryUI;
    public StatusBarManager statusBarManager;

    private float currentOxygen;
    public bool IsGameOver { get; private set; } = false;

    // 装備中の酸素タンクインスタンス（EquipmentSystemから設定）
    public OxygenTankInstance equippedTank = null;

    void Start()
    {
        currentOxygen = maxOxygen;
        UpdateBar();
    }

    void Update()
    {
        if (IsGameOver) return;

        bool hasAir = EnvironmentSystem.Instance != null && EnvironmentSystem.Instance.HasAir;
        bool tankHasOxygen = equippedTank != null && equippedTank.HasOxygen;

        if (hasAir)
        {
            // 空気あり：個人バーを急速回復。タンク消費なし
            RecoverOxygen(recoveryRate);
        }
        else if (tankHasOxygen)
        {
            // 空気なし＋タンクあり：タンクから消費しつつ個人バーを維持
            // （船内大気枯渇・船外どちらも同じ挙動）
            RecoverOxygen(recoveryRate);
            equippedTank.currentOxygen -= drainRate * Time.deltaTime;
            equippedTank.currentOxygen = Mathf.Clamp(
                equippedTank.currentOxygen, 0f, equippedTank.data.maxTankOxygen);
        }
        else
        {
            // 空気なし＋タンクなし or タンク切れ：個人バーが急速減少
            DrainOxygen(fastDrainRate);
        }

        UpdateBar();

        if (currentOxygen <= maxOxygen * 0.2f)
            Debug.Log("警告：酸素残量低下");

        if (currentOxygen <= 0f)
            GameOver();
    }

    void RecoverOxygen(float rate)
    {
        currentOxygen += rate * Time.deltaTime;
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
    }

    void DrainOxygen(float rate)
    {
        currentOxygen -= rate * Time.deltaTime;
        currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
    }

    public void AddOxygen(float amount)
    {
        currentOxygen = Mathf.Clamp(currentOxygen + amount, 0f, maxOxygen);
        UpdateBar();
    }

    public void ExpandMaxOxygen(float amount)
    {
        maxOxygen += amount;
        if (oxygenBar != null) oxygenBar.maxValue = maxOxygen;
    }

    public void ModifyDrainRate(float multiplier)
    {
        drainRate *= multiplier;
    }

    public void Revive()
    {
        currentOxygen = maxOxygen;
        IsGameOver = false;
        UpdateBar();
    }

    void UpdateBar()
    {
        if (statusBarManager != null)
            statusBarManager.SetValue("oxygen", currentOxygen / maxOxygen);
    }

    void GameOver()
    {
        IsGameOver = true;
        if (inventoryUI != null)
            inventoryUI.CloseInventory();
        if (crewSystem != null)
            crewSystem.OnPlayerDeath();
    }
}