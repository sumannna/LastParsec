using UnityEngine;

[CreateAssetMenu(fileName = "NewPlaceable", menuName = "LastParsec/PlaceableData")]
public class PlaceableData : ItemData
{
    [Header("設置設定")]
    public GameObject placePrefab;      // 設置されるPrefab
    public float placeDistance = 3f;    // 設置可能距離
    public LayerMask placeLayerZeroG;       // 無重力：床・壁・天井・設備
    public LayerMask placeLayerGravity;     // 遠心重力：床のみ
}