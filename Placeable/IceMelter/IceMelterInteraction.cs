using UnityEngine;

public class IceMelterInteraction : MonoBehaviour
{
    [Header("設定")]
    public float interactRange = 2f;

    [Header("参照")]
    public Transform playerTransform;
    public IceMelter iceMelter;

    void Update()
    {
        bool ePressed = Input.GetKeyDown(KeyCode.E);
        bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
        if (!ePressed && !tabPressed) return;

        // 閉じる：範囲チェック不要
        if (IceMelterUI.Instance != null && IceMelterUI.Instance.IsOpen)
        {
            IceMelterUI.Instance.Close();
            return;
        }

        // Open: Eキーのみ、Tabでは開かない
        if (!ePressed) return;
        if (!IsPlayerInRange()) return;
        if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
            UIManager.Instance?.OpenIceMelter(this);
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    public IceMelter GetMachine() => iceMelter;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}