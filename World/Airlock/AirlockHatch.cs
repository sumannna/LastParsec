using System.Collections;
using UnityEngine;

/// <summary>
/// エアロックの個別ハッチ。Open/Closeアニメーションを制御する。
/// Airlock から操作される。
/// </summary>
public class AirlockHatch : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float openDuration = 2f;
    [SerializeField] private bool startOpen = false;

    [Header("開閉時の移動設定（スライド式）")]
    [SerializeField] private Vector3 openOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private Vector3 closedOffset = new Vector3(0f, 0f, 0f);

    public bool IsOpen { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    private Vector3 openLocalPos;
    private Vector3 closedLocalPos;

    void Start()
    {
        closedLocalPos = transform.localPosition + closedOffset;
        openLocalPos = transform.localPosition + openOffset;

        if (startOpen)
        {
            transform.localPosition = openLocalPos;
            IsOpen = true;
        }
        else
        {
            transform.localPosition = closedLocalPos;
            IsOpen = false;
        }
    }

    public void Open(System.Action onComplete = null)
    {
        if (IsOpen || IsMoving) return;
        StartCoroutine(MoveHatch(true, onComplete));
    }

    public void Close(System.Action onComplete = null)
    {
        if (!IsOpen || IsMoving) return;
        StartCoroutine(MoveHatch(false, onComplete));
    }

    IEnumerator MoveHatch(bool opening, System.Action onComplete)
    {
        IsMoving = true;
        Vector3 from = transform.localPosition;
        Vector3 to = opening ? openLocalPos : closedLocalPos;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            transform.localPosition = Vector3.Lerp(from, to, elapsed / openDuration);
            yield return null;
        }

        transform.localPosition = to;
        IsOpen = opening;
        IsMoving = false;
        onComplete?.Invoke();
    }
}