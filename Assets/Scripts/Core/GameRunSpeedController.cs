using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 测试用运行倍速控制器：数字键 1~5 切换倍速（仅 Editor / Development Build）。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>GameSystems</c>（由 <see cref="GameBootstrapper"/> 自动创建）。</para>
/// <para>倍速仅影响 <see cref="Time.timeScale"/>，局内公式与曲线不变。</para>
/// </remarks>
public class GameRunSpeedController : MonoBehaviour, IGameSystem
{
    [Header("Hotkeys")]
    [Tooltip("启用后可用数字键 1~5 与 +/- 调整运行倍速。")]
    [SerializeField] private bool enableHotkeys = true;

    private bool isInitialized;

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>订阅状态变更以便在恢复 Playing 时重新应用倍速。</summary>
    public void Initialize()
    {
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        isInitialized = true;
    }

    /// <summary>检测快捷键输入。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized || !enableHotkeys || !IsHotkeyInputAllowed())
        {
            return;
        }

        PollHotkeys();
    }

    /// <summary>取消订阅并重置倍速。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameRunSpeedSettings.Reset();
        isInitialized = false;
    }

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        GameRunSpeedSettings.ApplyToUnityTimeScale(change.NewState);
    }

    private static bool IsHotkeyInputAllowed()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return true;
#else
        return false;
#endif
    }

    private static void PollHotkeys()
    {
#if ENABLE_INPUT_SYSTEM
        PollHotkeysInputSystem();
#else
        PollHotkeysLegacyInput();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private static void PollHotkeysInputSystem()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame) GameRunSpeedSettings.SetPlaySpeedMultiplier(1f);
        if (keyboard.digit2Key.wasPressedThisFrame) GameRunSpeedSettings.SetPlaySpeedMultiplier(2f);
        if (keyboard.digit3Key.wasPressedThisFrame) GameRunSpeedSettings.SetPlaySpeedMultiplier(3f);
        if (keyboard.digit4Key.wasPressedThisFrame) GameRunSpeedSettings.SetPlaySpeedMultiplier(4f);
        if (keyboard.digit5Key.wasPressedThisFrame) GameRunSpeedSettings.SetPlaySpeedMultiplier(5f);

        if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
        {
            StepMultiplier(1f);
        }

        if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
        {
            StepMultiplier(-1f);
        }
    }
#endif

    private static void PollHotkeysLegacyInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) GameRunSpeedSettings.SetPlaySpeedMultiplier(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2)) GameRunSpeedSettings.SetPlaySpeedMultiplier(2f);
        if (Input.GetKeyDown(KeyCode.Alpha3)) GameRunSpeedSettings.SetPlaySpeedMultiplier(3f);
        if (Input.GetKeyDown(KeyCode.Alpha4)) GameRunSpeedSettings.SetPlaySpeedMultiplier(4f);
        if (Input.GetKeyDown(KeyCode.Alpha5)) GameRunSpeedSettings.SetPlaySpeedMultiplier(5f);

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            StepMultiplier(1f);
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            StepMultiplier(-1f);
        }
    }

    private static void StepMultiplier(float delta)
    {
        float next = GameRunSpeedSettings.PlaySpeedMultiplier + delta;
        GameRunSpeedSettings.SetPlaySpeedMultiplier(next);
    }
}
