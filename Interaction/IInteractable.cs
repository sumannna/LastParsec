/// <summary>
/// Eキーでインタラクト可能なオブジェクトの共通インターフェース。
/// InteractionManager が毎フレーム Raycast で検出し、
/// フォーカス管理・ハイライト・プロンプト表示・Eキー処理を一元管理する。
/// </summary>
public interface IInteractable
{
    /// <summary>画面に表示するラベル（例："開く [E]"）</summary>
    string InteractionLabel { get; }

    /// <summary>現在 E キーで操作可能か（false でもハイライトは有効）</summary>
    bool CanInteract { get; }

    void Interact();
    void OnFocusEnter();
    void OnFocusExit();
}