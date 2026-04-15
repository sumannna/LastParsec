using UnityEngine;

/// <summary>
/// カメラ前方に Raycast を発射し、IInteractable を検出して
/// ハイライト・プロンプト表示・Eキー入力を一元管理する Singleton。
/// </summary>
public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    [Header("設定")]
    [SerializeField] private float maxInteractDistance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("参照")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private VitalSystem vitalSystem;
    [SerializeField] private InteractionPromptUI promptUI;

    private IInteractable currentFocus;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        bool isDead = vitalSystem != null && vitalSystem.IsDead;
        bool anyUIOpen = UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen();

        if (isDead || anyUIOpen)
        {
            ClearFocus();
            return;
        }

        IInteractable found = null;
        if (cameraTransform != null)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            Debug.DrawRay(ray.origin, ray.direction * maxInteractDistance, Color.red);

            if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance))
            {
                if (Physics.Raycast(ray, out RaycastHit hit2, maxInteractDistance, interactableLayer))
                {
                    found = hit2.collider.GetComponent<IInteractable>();
                }
            }
        }

        if (found != currentFocus)
        {
            currentFocus?.OnFocusExit();
            currentFocus = found;
            currentFocus?.OnFocusEnter();
        }

        if (currentFocus != null)
        {
            promptUI?.Show(currentFocus.InteractionLabel);
            if (currentFocus.CanInteract && Input.GetKeyDown(KeyCode.E))
                currentFocus.Interact();
        }
        else
        {
            promptUI?.Hide();
        }
    }

    void ClearFocus()
    {
        if (currentFocus == null) return;
        currentFocus.OnFocusExit();
        currentFocus = null;
        promptUI?.Hide();
    }
}