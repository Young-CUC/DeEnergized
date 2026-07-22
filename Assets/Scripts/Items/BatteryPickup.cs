using UnityEngine;

/// <summary>
/// 备用电池拾取物 —— 场景中的可收集电池。
/// 碰撞触发交互，拾取后销毁自身。
/// </summary>
[RequireComponent(typeof(Collider))]
public class BatteryPickup : MonoBehaviour, IInteractable
{
    // ── IInteractable ──────────────────────

    public string GetPrompt() => InteractionTexts.BatteryPickup;

    public bool CanInteract(PlayerInventory inventory)
    {
        // 电池拾取无需条件
        return true;
    }

    public void OnInteract(PlayerInventory inventory)
    {
        inventory.AddBattery();
        Destroy(gameObject);
    }

    // ── 确保碰撞体为触发器 ────────────────

    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }
}
