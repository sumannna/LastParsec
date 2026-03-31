using UnityEngine;

public class FillingMachineInteraction : MonoBehaviour
{
    public float interactRange = 2f;
    public Transform playerTransform;
    public FillingMachine fillingMachine;

    void Update()
    {
        if (!IsPlayerInRange()) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (FillingMachineUI.Instance == null) return;

        if (FillingMachineUI.Instance.IsOpen)
            FillingMachineUI.Instance.Close();
        else if (UIManager.Instance == null || !UIManager.Instance.IsAnyUIOpen())
            UIManager.Instance?.OpenFillingMachine(this);
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    public FillingMachine GetMachine() => fillingMachine;
}