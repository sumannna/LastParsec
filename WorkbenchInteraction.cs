using UnityEngine;

/// <summary>
/// ワークベンチに付けるコンポーネント。
/// - プレイヤーが1m以内 + Eキー → クラフトツリー画面を開く
/// - CraftSystemからIsPlayerInRange()で距離判定に使用される
/// </summary>
[RequireComponent(typeof(Collider))]
public class WorkbenchInteraction : MonoBehaviour
{
    [Header("WBレベル")]
    public WBLevel wbLevel = WBLevel.Lv1;

    [Header("インタラクト設定")]
    [SerializeField] private float interactRange = 1f;  // 仕様：1m以内

    [Header("参照")]
    [SerializeField] public Transform playerTransform;

    // -----------------------------------------------
    // 毎フレーム更新
    // -----------------------------------------------

    void Update()
    {
        if (!IsPlayerInRange()) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (CraftTreeUI.Instance != null && CraftTreeUI.Instance.IsOpen)
                CraftTreeUI.Instance.Close();
            else if (UIManager.Instance == null || (!UIManager.Instance.IsAnyUIOpen()))
                OpenCraftTree();
        }
    }

    // -----------------------------------------------
    // 公開API
    // -----------------------------------------------

    /// <summary>
    /// プレイヤーがWBの有効範囲内にいるか判定する。
    /// CraftSystemが毎フレーム呼ぶ。
    /// </summary>
    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        float dist = Vector3.Distance(playerTransform.position, transform.position);
        return dist <= interactRange;
    }

    // -----------------------------------------------
    // 内部処理
    // -----------------------------------------------

    private void OpenCraftTree()
    {
        Debug.Log($"[WorkbenchInteraction] クラフトツリーを開く WBLevel:{wbLevel}");

        if (UIManager.Instance != null)
            UIManager.Instance.OpenCraftTree(this);
        else if (CraftTreeUI.Instance != null)
            CraftTreeUI.Instance.Open(this);
        else
            Debug.LogWarning("[WorkbenchInteraction] CraftTreeUIが見つかりません");
    }

    // -----------------------------------------------
    // Gizmo（Scene上で範囲を可視化）
    // -----------------------------------------------

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}