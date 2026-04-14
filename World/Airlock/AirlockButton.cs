using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// エアロック操作ボタン。船内側・船外側・エアロック内に設置する。
/// ボタンのアクションタイプを設定し、Airlock に委譲する。
/// </summary>
public class AirlockButton : MonoBehaviour
{
    public enum ActionType
    {
        Pressurize,   // 空気を入れる
        Depressurize, // 空気を抜く
    }

    [Header("設定")]
    [SerializeField] private ActionType actionType;
    [SerializeField] private Airlock airlock;
    [SerializeField] private float interactDistance = 2f;

    [Header("参照")]
    [SerializeField] private Transform playerTransform;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (airlock == null) return;
        if (playerTransform != null &&
            Vector3.Distance(playerTransform.position, transform.position) > interactDistance) return;

        if (actionType == ActionType.Pressurize)
            airlock.RequestPressurize();
        else
            airlock.RequestDepressurize();
    }
}