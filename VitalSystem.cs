using UnityEngine;

public class VitalSystem : MonoBehaviour
{
    [Header("HP設定")]
    public float maxHP = 100f;
    public float downDuration = 10f; // ダウン後の猶予時間

    [Header("空腹設定")]
    public float maxHunger = 100f;
    public float hungerDrainRate = 2f;
    public float hungerHPDrainRate = 3f; // 空腹0からのHP減少速度

    [Header("水分設定")]
    public float maxWater = 100f;
    public float waterDrainRate = 3f;
    public float waterHPDrainRate = 2f; // 水0からのHP減少速度

    [Header("参照")]
    public StatusBarManager statusBarManager;
    public CrewSystem crewSystem;
    public InventoryUI inventoryUI;

    private float currentHP;
    private float currentHunger;
    private float currentWater;
    private bool isDown = false;
    private float downTimer = 0f;
    public bool IsDead { get; private set; } = false;

    void Start()
    {
        currentHP = maxHP;
        currentHunger = maxHunger;
        currentWater = maxWater;
        UpdateBars();
    }

    void Update()
    {
        if (IsDead) return;

        // ダウン処理
        if (isDown)
        {
            downTimer -= Time.deltaTime;
            if (downTimer <= 0f)
                Die();
            return;
        }

        // 空腹・水分の減少
        currentHunger -= hungerDrainRate * Time.deltaTime;
        currentHunger = Mathf.Clamp(currentHunger, 0f, maxHunger);

        currentWater -= waterDrainRate * Time.deltaTime;
        currentWater = Mathf.Clamp(currentWater, 0f, maxWater);

        // HP減少（空腹・水分が0のとき）
        float hpDrain = 0f;
        if (currentHunger <= 0f) hpDrain += hungerHPDrainRate;
        if (currentWater <= 0f) hpDrain += waterHPDrainRate;

        if (hpDrain > 0f)
        {
            currentHP -= hpDrain * Time.deltaTime;
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        }

        // HPが0でダウン
        if (currentHP <= 0f && !isDown)
            StartDown();

        UpdateBars();
    }

    void StartDown()
    {
        isDown = true;
        downTimer = downDuration;
        Debug.Log($"ダウン：{downDuration}秒以内に回復しないと死亡");
    }

    void Die()
    {
        IsDead = true;
        Debug.Log("死亡：HP切れ");
        if (inventoryUI != null)
            inventoryUI.CloseInventory();

        if (crewSystem != null)
            crewSystem.OnPlayerDeath();
    }

    // ダメージを与える（装備・戦闘などから呼ぶ）
    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        currentHP -= amount;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        if (statusBarManager != null)
            statusBarManager.SetValue("hp", currentHP / maxHP);
    }

    // 回復時に回復（ダウン状態を解除）
    public void HealHP(float amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0f, maxHP);
        if (isDown && currentHP > 0f)
        {
            isDown = false;
            downTimer = 0f;
            Debug.Log("ダウン解除");
        }
    }

    public void AddHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0f, maxHunger);
    }

    public void AddWater(float amount)
    {
        currentWater = Mathf.Clamp(currentWater + amount, 0f, maxWater);
    }

    public void Revive()
    {
        IsDead = false;
        isDown = false;
        downTimer = 0f;
        currentHP = maxHP;
        currentHunger = maxHunger;
        currentWater = maxWater;
        UpdateBars();
    }

    void UpdateBars()
    {
        if (statusBarManager == null) return;
        statusBarManager.SetValue("hp", currentHP / maxHP);
        statusBarManager.SetValue("hunger", currentHunger / maxHunger);
        statusBarManager.SetValue("water", currentWater / maxWater);
    }
}
