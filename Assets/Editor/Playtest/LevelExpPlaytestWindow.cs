#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Play / Edit 模式下编辑每级升级所需经验：单级固定值或全局倍率覆盖公式结果。
/// </summary>
public sealed class LevelExpPlaytestWindow : EditorWindow
{
    private const string WindowTitle = "Level Exp Test";
    private const string GameConfigPath = "Assets/Resources/Config/GameConfig.asset";

    private bool foldSettings = true;
    private int maxLevelToShow = 20;
    private Vector2 scroll;

    [MenuItem("Attack Barbarians/Playtest/Level Exp Test Window")]
    public static void Open()
    {
        LevelExpPlaytestWindow window = GetWindow<LevelExpPlaytestWindow>();
        window.titleContent = new GUIContent(WindowTitle);
        window.minSize = new Vector2(440f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    private void OnEditorUpdate()
    {
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("等级经验测试", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "覆盖 V2 等级曲线计算的升级门槛（与波次无关）。单级覆盖优先于全局倍率；覆盖值 ≤0 表示该级使用公式。\n" +
            "Play 模式下修改会立即影响经验条与升级判定（Editor / Development Build）。",
            MessageType.Info);

        DrawSettingsSection();
        EditorGUILayout.Space(6f);
        DrawLevelTable();
        EditorGUILayout.Space(4f);
        DrawFooter();
    }

    private void DrawSettingsSection()
    {
        foldSettings = EditorGUILayout.BeginFoldoutHeaderGroup(foldSettings, "覆盖设置");
        if (!foldSettings)
        {
            EditorGUILayout.EndFoldoutHeaderGroup();
            return;
        }

        EditorGUI.BeginDisabledGroup(!LevelExpPlaytestSettings.IsFeatureAvailable);
        bool enabled = LevelExpPlaytestSettings.IsEnabled;
        EditorGUI.BeginChangeCheck();
        enabled = EditorGUILayout.Toggle("启用等级经验覆盖", enabled);
        if (EditorGUI.EndChangeCheck())
        {
            LevelExpPlaytestSettings.SetEnabled(enabled);
        }

        EditorGUI.BeginChangeCheck();
        float globalMult = LevelExpPlaytestSettings.GlobalMultiplier;
        globalMult = EditorGUILayout.Slider("全局倍率（无单级覆盖时）", globalMult, 0.01f, 10f);
        if (EditorGUI.EndChangeCheck())
        {
            LevelExpPlaytestSettings.SetGlobalMultiplier(globalMult);
        }

        EditorGUI.BeginChangeCheck();
        maxLevelToShow = EditorGUILayout.IntSlider("显示等级数", maxLevelToShow, 5, LevelExpPlaytestSettings.MaxEditableLevel);
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndFoldoutHeaderGroup();

        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField(
                $"运行中：玩家 L={ResolveCurrentPlayerLevel()}，" +
                $"波次 W={RunProgressionContext.CurrentWave}（仅影响击杀经验），" +
                $"难度门槛倍率 ×{RunDifficultyContext.ExpNeedDifficultyMult:0.##}");
        }
    }

    private void DrawLevelTable()
    {
        WaveProgressionConfigSO progression = ResolveProgressionConfig();
        if (progression == null)
        {
            EditorGUILayout.HelpBox("未找到 WaveProgressionConfig，请检查 GameConfig 引用。", MessageType.Warning);
            return;
        }

        float difficultyMult = Application.isPlaying
            ? RunDifficultyContext.ExpNeedDifficultyMult
            : 1f;
        int currentLevel = ResolveCurrentPlayerLevel();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("等级", GUILayout.Width(52f));
        EditorGUILayout.LabelField("公式值", GUILayout.Width(72f));
        EditorGUILayout.LabelField("覆盖值 (0=公式)", GUILayout.MinWidth(120f));
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int level = 1; level <= maxLevelToShow; level++)
        {
            DrawLevelRow(level, progression, difficultyMult, currentLevel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawLevelRow(
        int level,
        WaveProgressionConfigSO progression,
        float difficultyMult,
        int currentLevel)
    {
        float formulaBase = ComputeFormulaNeed(level, progression, difficultyMult);

        LevelExpPlaytestSettings.TryGetLevelOverride(level, out float overrideValue);
        bool hasOverride = overrideValue > 0f;
        float effectiveNeed = LevelExpPlaytestSettings.IsEnabled
            ? LevelExpPlaytestSettings.ResolveNeedExperience(formulaBase, level)
            : formulaBase;

        bool isCurrent = Application.isPlaying && level == currentLevel;
        if (isCurrent)
        {
            EditorGUILayout.BeginVertical("box");
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{level}→{level + 1}", GUILayout.Width(52f));
        EditorGUILayout.LabelField($"{formulaBase:0}", GUILayout.Width(72f));

        EditorGUI.BeginDisabledGroup(!LevelExpPlaytestSettings.IsFeatureAvailable);
        EditorGUI.BeginChangeCheck();
        float newOverride = EditorGUILayout.FloatField(hasOverride ? overrideValue : 0f);
        if (EditorGUI.EndChangeCheck())
        {
            LevelExpPlaytestSettings.SetLevelOverride(level, newOverride);
            Repaint();
        }

        EditorGUI.EndDisabledGroup();

        string suffix = isCurrent ? " ◀ 当前" : string.Empty;
        EditorGUILayout.LabelField($"→ {effectiveNeed:0}{suffix}", GUILayout.Width(100f));
        EditorGUILayout.EndHorizontal();

        if (isCurrent)
        {
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(!LevelExpPlaytestSettings.IsFeatureAvailable);
        if (GUILayout.Button("清除全部单级覆盖", GUILayout.Height(24f)))
        {
            LevelExpPlaytestSettings.ClearAllLevelOverrides();
            Repaint();
        }

        if (GUILayout.Button("关闭覆盖并重置", GUILayout.Height(24f)))
        {
            LevelExpPlaytestSettings.Reset();
            Repaint();
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (!LevelExpPlaytestSettings.IsFeatureAvailable)
        {
            EditorGUILayout.HelpBox("等级经验测试仅在 Editor 或 Development Build 中可用。", MessageType.Warning);
        }
    }

    private static float ComputeFormulaNeed(
        int level,
        WaveProgressionConfigSO cfg,
        float difficultyExpMult)
    {
        if (cfg == null)
        {
            return 100f;
        }

        level = Mathf.Max(1, level);
        float mult = Mathf.Max(0.01f, difficultyExpMult);

        float baseNeed = cfg.ExpBase * Mathf.Pow(level, cfg.ExpGrowthPower) *
                         Mathf.Exp(cfg.ExpLambda * Mathf.Max(0, level - cfg.ExpLambdaStartLevel));
        return Mathf.Max(1f, baseNeed * mult);
    }

    private static WaveProgressionConfigSO ResolveProgressionConfig()
    {
        if (Application.isPlaying && RunProgressionContext.Config != null)
        {
            return RunProgressionContext.Config;
        }

        GameConfig gameConfig = AssetDatabase.LoadAssetAtPath<GameConfig>(GameConfigPath);
        if (gameConfig == null)
        {
            return null;
        }

        if (gameConfig.WaveProgressionConfig != null)
        {
            return gameConfig.WaveProgressionConfig;
        }

        ConfigDatabaseSO database = gameConfig.ConfigDatabase;
        return database != null ? database.WaveProgression : null;
    }

    private static int ResolveCurrentPlayerLevel()
    {
        if (!Application.isPlaying || !PlayerSceneAccess.TryGetController(out PlayerController controller) ||
            !controller.IsReady || controller.ActiveData == null)
        {
            return 0;
        }

        return controller.RuntimeStats.Data.CurrentLevel;
    }
}
#endif
