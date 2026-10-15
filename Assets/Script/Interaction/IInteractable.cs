
public interface IInteractable
{
    // 顯示互動提示
    string InteractionPrompt { get; }

    // 執行互動
    void Interact();
}
