using UnityEngine;

/// <summary>
/// 兼容层：旧场景仍挂 <c>PlayerCombat</c> 时转发到 <see cref="PlayerCombatBridge"/>。
/// </summary>
/// <remarks>新场景请改用 <see cref="PlayerCombatBridge"/> 并移除本组件。</remarks>
[System.Obsolete("Use PlayerCombatBridge on Player. Target scanning moved to PlayerController + CollisionManager.")]
public class PlayerCombat : PlayerCombatBridge
{
}
