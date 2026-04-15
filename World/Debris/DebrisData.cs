using UnityEngine;

/// <summary>
/// 漂流物 1 種の定義。ScriptableObject として Assets に作成して使用。
/// Assets > Create > World > DebrisData
/// </summary>
[CreateAssetMenu(fileName = "NewDebris", menuName = "World/DebrisData")]
public class DebrisData : ScriptableObject
{
    [Header("基本情報")]
    public string debrisName = "漂流物";
    public GameObject worldPrefab;

    [Header("取得設定")]
    public bool requiresPickaxe = true;
    public ItemData dropItem;

    [Header("ツルハシ取得（requiresPickaxe = true のとき有効）")]
    public int maxYield = 100;
    public int yieldPerHit = 12;

    [Header("E取得（requiresPickaxe = false のとき有効）")]
    public int yieldPerPickup = 10;

    [Header("飛来設定")]
    public float approachSpeedMin = 3f;
    public float approachSpeedMax = 8f;

    [Header("飛翔設定")]
    public float despawnDistance = 300f; // 累積移動距離でDestroyする

    [Header("自転設定")]
    public float rotationSpeedMin = 5f;
    public float rotationSpeedMax = 25f;
}