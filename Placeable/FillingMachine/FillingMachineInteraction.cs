using UnityEngine;

public class FillingMachineInteraction : MonoBehaviour
{
    public float interactRange = 2f;
    public Transform playerTransform;
    public FillingMachine fillingMachine;

    void Update()
    {
        bool ePressed = Input.GetKeyDown(KeyCode.E);
        bool tabPressed = Input.GetKeyDown(KeyCode.Tab);
        if (!ePressed && !tabPressed) return;
        if (FillingMachineUI.Instance == null) return;

        // 自分のUIが開いている場合のみ閉じる
        if (FillingMachineUI.Instance.IsOpen && FillingMachineUI.Instance.CurrentMachine == fillingMachine)
        {
            FillingMachineUI.Instance.Close();
            return;
        }

        // 開く：Eキー・範囲内・自分のUIが開いていない・他UIが開いていない
        if (!ePressed) return;
        if (!IsPlayerInRange()) return;
        if (FillingMachineUI.Instance.IsOpen) return;
        if (FillingMachineUI.Instance.ClosedThisFrame) return;
        if (!UIManager.Instance.IsAnyUIOpen())
            UIManager.Instance?.OpenFillingMachine(this);
    }

    public bool IsPlayerInRange()
    {
        if (playerTransform == null) return false;
        return Vector3.Distance(playerTransform.position, transform.position) <= interactRange;
    }

    public FillingMachine GetMachine() => fillingMachine;
}