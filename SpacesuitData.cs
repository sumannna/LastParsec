using UnityEngine;

[CreateAssetMenu(fileName = "NewSpacesuit", menuName = "Inventory/SpacesuitData")]
public class SpacesuitData : ItemData
{
    [Header("宇宙服設定")]
    public float maxDurability = 100f;
    public float damageReduction = 0.5f; // ダメージ軽減率（0〜1）
}
