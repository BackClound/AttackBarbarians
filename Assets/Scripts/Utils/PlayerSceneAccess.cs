using UnityEngine;

/// <summary>
/// 场景内 Player 访问工具：优先单例，避免散落 <c>FindObjectOfType</c>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。</para>
/// <para><b>使用方式：</b>Meta/Upgrade 系统在 Bootstrap 后通过本类获取 <see cref="PlayerController"/>。</para>
/// </remarks>
public static class PlayerSceneAccess
{
    /// <summary>当前场景是否存在已初始化的 Player。</summary>
    public static bool HasReadyPlayer =>
        TryGetController(out PlayerController controller) && controller.IsReady;

    /// <summary>尝试获取 Player 实体。</summary>
    public static bool TryGetPlayer(out Player player)
    {
        player = null;
        if (!Player.HasInstance)
        {
            return false;
        }

        player = Player.Instance;
        return player != null;
    }

    /// <summary>尝试获取 PlayerController（须 IsReady）。</summary>
    public static bool TryGetController(out PlayerController controller)
    {
        controller = null;
        if (!TryGetPlayer(out Player player))
        {
            return false;
        }

        controller = player.controller;
        return controller != null && controller.IsReady;
    }

    /// <summary>尝试获取 PlayerSkillManager。</summary>
    public static bool TryGetSkillManager(out PlayerSkillManager skillManager)
    {
        skillManager = null;
        if (!TryGetPlayer(out Player player))
        {
            return false;
        }

        skillManager = player.skillManager;
        return skillManager != null;
    }

    /// <summary>尝试获取 SkillManager（射击/技能 Buff 入口）。</summary>
    public static bool TryGetSkillSystem(out SkillManager skillManager)
    {
        skillManager = null;
        if (!TryGetPlayer(out Player player))
        {
            return false;
        }

        skillManager = player.GetComponent<SkillManager>();
        return skillManager != null;
    }

    /// <summary>尝试获取 Player_Health。</summary>
    public static bool TryGetHealth(out Player_Health health)
    {
        health = null;
        if (!TryGetPlayer(out Player player))
        {
            return false;
        }

        health = player.GetComponent<Player_Health>();
        return health != null;
    }

    /// <summary>对就绪的 PlayerController 执行回调。</summary>
    public static void WithReadyController(System.Action<PlayerController> action)
    {
        if (action == null || !TryGetController(out PlayerController controller))
        {
            return;
        }

        action(controller);
    }
}
