using UnityEngine;

public class IceMelterInteraction : MonoBehaviour
{
    [Header("ê›íË")]
    public float interactRange = 2f;

    [Header("éQè∆")]
    public Transform playerTransform;
    public IceMelter iceMelter;

    void Update()
    {
        if (!IsPlayerInRange()) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            float dist = playerTransform != null
                ? Vector3.Distance(playerTransform.position, transform.position)
                : -1f;
            Debug.Log($"[IceMelterInteraction] E pressed / inRange={IsPlayerInRange()} / dist={dist:F2} / IceMelterUI.Instance={IceMelterUI.Instance != null} / IsOpen={IceMelterUI.Instance?.IsOpen} / IsAnyUIOpen={UIManager.Instance?.IsAnyUIOpen()}");
        }

        if (IceMelterUI.Instance == null)
        {
            Debug.Log("[IceMelterInteraction] IceMelterUI.Instance Ç™ null");
            return;
        }

        if (IceMelterUI.Instance.IsOpen)
            IceMelterUI.Instance.Close();
        else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
        {
            Debug.Log("[IceMelterInteraction] OpenIceMelter åƒÇ—èoÇµ");
            UIManager.Instance?.OpenIceMelter(this);
        }
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