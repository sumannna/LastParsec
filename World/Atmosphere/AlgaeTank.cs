using UnityEngine;

/// <summary>
/// 藻水槽。藻アイテムをスロットに投入している間、船内に酸素を供給する。
/// Placeableとしてワールドに設置する。
/// </summary>
public class AlgaeTank : MonoBehaviour, IOxygenSupplier
{
    [Header("設定")]
    [SerializeField] private float generationRate = 3f; // m³/h

    [Header("藻スロット")]
    [SerializeField] private ItemData algaeItemData;

    // 藻スロット（1スロット）
    private Inventory.Slot algaeSlot;

    // IOxygenSupplier
    public bool IsSupplying => algaeSlot != null && algaeSlot.item == algaeItemData && algaeSlot.amount > 0;
    public float OxygenGeneration => generationRate;

    // 外部からスロットを参照するためのプロパティ（UI用）
    public Inventory.Slot AlgaeSlot => algaeSlot;

    void Start()
    {
        ShipAtmosphereSystem.Instance?.RegisterSupplier(this);
    }

    void OnDestroy()
    {
        ShipAtmosphereSystem.Instance?.UnregisterSupplier(this);
    }

    /// <summary>藻を投入する（UIから呼ぶ）</summary>
    public bool InsertAlgae(Inventory.Slot slot)
    {
        if (slot == null || slot.item != algaeItemData) return false;
        algaeSlot = slot;
        return true;
    }

    /// <summary>藻を取り出す（UIから呼ぶ）</summary>
    public Inventory.Slot ExtractAlgae()
    {
        var slot = algaeSlot;
        algaeSlot = null;
        return slot;
    }
}