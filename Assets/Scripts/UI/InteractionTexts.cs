/// <summary>
/// 交互提示文本 —— 集中管理所有 UI 字符串。
/// 修改文本只需改此处，无需逐个脚本查找。
/// </summary>
using UnityEngine;

public static class InteractionTexts
{
    // ── Colors ────────────────────────────
    public static readonly Color DefaultColor = Color.white;
    public static readonly Color WarnColor = new Color(1f, 0.3f, 0.2f);  // red-orange

    // ── Battery Pickup ────────────────────
    public const string BatteryPickup = "Press 'E' to pick up Spare Battery";

    // ── Battery Slot ──────────────────────
    public const string SlotInsert = "Press 'E' to insert Spare Battery";
    public const string SlotRemove = "Press 'E' to remove Spare Battery";
    public const string SlotNeedBattery = "Spare Battery required";

    // ── Player Inventory ──────────────────
    public const string HasBattery = "Spare Battery";
    public const string NoBattery = "";

    // ── Distribution Panel ────────────────
    public const string PanelReady = "Press 'E' to insert Core Battery";
    public const string PanelNoBattery = "Core Battery required";
    public const string PanelUsed = "Panel activated";
}
