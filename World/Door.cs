using System.Collections;
using UnityEngine;

/// <summary>
/// 横スライドドア。
/// 範囲内でEキーを押すたびに開閉トグルする。
/// </summary>
public class Door : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private float openDuration = 0.5f;
    [SerializeField] private Vector3 openOffset = new Vector3(2f, 0f, 0f);
    [SerializeField] private bool startOpen = false;

    [Header("参照")]
    [SerializeField] private Transform playerTransform;

    public bool IsOpen { get; private set; } = false;
    public bool IsMoving { get; private set; } = false;

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    void Start()
    {
        closedLocalPos = transform.localPosition;
        openLocalPos = transform.localPosition + openOffset;

        if (startOpen)
        {
            transform.localPosition = openLocalPos;
            IsOpen = true;
        }
    }

    void Update()
    {
        if (!IsPlayerInRange()) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        Toggle();
    }

    void Toggle()
    {
        if (IsMoving) return;
        if (IsOpen) StartCoroutine(Slide(false));
        else StartCoroutine(Slide(true));
    }

    IEnumerator Slide(bool opening)
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
    }

    bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + openOffset, transform.lossyScale);
    }
#endif
}