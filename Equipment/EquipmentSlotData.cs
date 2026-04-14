using UnityEngine;

[CreateAssetMenu(fileName = "NewEquipmentSlot", menuName = "Inventory/EquipmentSlotData")]
public class EquipmentSlotData : ScriptableObject
{
    [Header("スロット定義")]
    public string slotName;         // 例：「宇宙服」
    public ItemType acceptedType;   // このスロットに装備できるItemType
    public Sprite slotIcon;         // 空のときに表示するアイコン
}
