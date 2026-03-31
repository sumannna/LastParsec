using System.Collections;
using UnityEngine;

/// <summary>
/// 採掘対象オブジェクト。岩・氷などに付与する。
/// Pickaxeでダメージを受けてHPが0になるとドロップして非表示（一定時間で復活）。
///
/// ■ Unity Editor設定
/// 1. 岩・氷などのGameObjectにアタッチ
/// 2. dropItem：ドロップするItemDataをアサイン
/// 3. このオブジェクトを PickaxeData.miningLayer に対応するレイヤーに設定
/// 4. Colliderが必要（Raycastが当たるため）
/// </summary>
public class MiningTarget : MonoBehaviour
{
    [Header("採掘設定")]
    public float maxHP = 100f;
    public float respawnTime = 30f;    // 復活までの秒数（0以下で復活しない）

    [Header("ドロップ設定")]
    public ItemData dropItem;
    public int dropAmountMin = 1;
    public int dropAmountMax = 3;

    [Header("参照")]
    public Inventory inventory;
    public InventoryUI inventoryUI;

    private float currentHP;
    private bool isMined = false;

    private Renderer[] renderers;
    private Collider[] cols;

    void Awake()
    {
        currentHP = maxHP;
        renderers = GetComponentsInChildren<Renderer>();
        cols = GetComponentsInChildren<Collider>();
    }

    /// <summary>
    /// ToolUserから呼ばれる。damageを与えてHPが0になればドロップ処理。
    /// </summary>
    public void TakeMiningDamage(float damage)
    {
        if (isMined) return;

        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        Debug.Log($"[MiningTarget] {gameObject.name} HP: {currentHP}/{maxHP}");

        if (currentHP <= 0f)
            StartCoroutine(MineCoroutine());
    }

    IEnumerator MineCoroutine()
    {
        isMined = true;
        SetVisible(false);

        // ドロップ処理
        if (dropItem != null && inventory != null)
        {
            int amount = Random.Range(dropAmountMin, dropAmountMax + 1);
            inventory.AddItemAmount(dropItem, amount);
            Debug.Log($"[MiningTarget] {dropItem.itemName} x{amount} をドロップ");

            if (inventoryUI != null && inventoryUI.IsOpen)
                inventoryUI.RefreshAll();
        }

        if (respawnTime <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        yield return new WaitForSeconds(respawnTime);

        currentHP = maxHP;
        isMined = false;
        SetVisible(true);
    }

    void SetVisible(bool visible)
    {
        foreach (var r in renderers) r.enabled = visible;
        foreach (var c in cols) c.enabled = visible;
    }
}