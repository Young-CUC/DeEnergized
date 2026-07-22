/// <summary>
/// 交互接口 —— 任何可被玩家按 E 使用的物体都实现此接口。
/// 单一职责：定义交互契约，不包含任何实现逻辑。
/// </summary>
public interface IInteractable
{
    /// <summary>交互提示文字。</summary>
    string GetPrompt();

    /// <summary>玩家当前是否可以与此物体交互。</summary>
    bool CanInteract(PlayerInventory inventory);

    /// <summary>执行交互逻辑。</summary>
    void OnInteract(PlayerInventory inventory);
}
