using UnityEngine;

public class HandCranker : MonoBehaviour, IBatterySource
{
    [Header("ê›íË")]
    public float maxCapacity = 10f;
    public float chargeRatePerSecond = 1f;

    public string SourceName => "HandCranker";
    public float MaxCapacity => maxCapacity;
    public float CurrentCharge => currentCharge;
    public float ChargeRatio => maxCapacity > 0f ? currentCharge / maxCapacity : 0f;

    private float currentCharge = 0f;
    private bool isPlayerNear = false;
    private bool isCranking = false;

    [Header("éQè∆")]
    public Transform playerTransform;
    public float interactRange = 2f;
    public InventoryUI inventoryUI;
    public HandCrankerUI crankerUI;

    public event System.Action OnChargeChanged;

    void Start()
    {
        PowerGridManager.Instance?.RegisterSource(this);
    }

    void OnDestroy()
    {
        PowerGridManager.Instance?.UnregisterSource(this);
    }

    void Update()
    {
        isPlayerNear = IsPlayerInRange();

        if (Input.GetKeyDown(KeyCode.E))
        {
            float dist = playerTransform != null
                ? Vector3.Distance(playerTransform.position, transform.position)
                : -1f;
        }

        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            if (crankerUI != null)
            {
                if (crankerUI.IsOpen)
                    crankerUI.Close();
                else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
                {
                    if (UIManager.Instance != null)
                        UIManager.Instance.OpenHandCranker(this);
                    else
                        crankerUI.Open(this);
                }
            }
        }

        if (inventoryUI != null && inventoryUI.IsOpen) return;

        isCranking = isPlayerNear && Input.GetMouseButton(0)
                     && crankerUI != null && crankerUI.IsOpen;

        if (isCranking)
        {
            Charge(chargeRatePerSecond * Time.deltaTime);
        }
    }

    public void Charge(float kWh)
    {
        currentCharge = Mathf.Clamp(currentCharge + kWh, 0f, maxCapacity);
        OnChargeChanged?.Invoke();
    }

    public float Discharge(float kWh)
    {
        float actual = Mathf.Min(kWh, currentCharge);
        currentCharge -= actual;
        OnChargeChanged?.Invoke();
        return actual;
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    public bool IsCranking => isCranking;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}