using UnityEngine;

public class ElectrolyzerInteraction : MonoBehaviour
{
    public float interactRange = 2f;
    public Transform playerTransform;
    public Electrolyzer electrolyzer;

    void Update()
    {
        if (!IsPlayerInRange()) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (ElectrolyzerUI.Instance == null) return;

        if (ElectrolyzerUI.Instance.IsOpen)
            ElectrolyzerUI.Instance.Close();
        else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
            UIManager.Instance?.OpenElectrolyzer(this);
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    public Electrolyzer GetMachine() => electrolyzer;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
#endif
}