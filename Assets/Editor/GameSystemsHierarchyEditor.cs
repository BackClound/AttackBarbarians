using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 按 <see cref="GameBootstrapper"/> Resolve 顺序维护 GameSystems 子物体层级与 Bootstrapper 引用。
/// </summary>
public static class GameSystemsHierarchyEditor
{
    private const string MainScenePath = "Assets/Scenes/MainScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";

    private readonly struct ManagerEntry
    {
        public readonly string ObjectName;
        public readonly Type ComponentType;
        public readonly string BootstrapField;
        public readonly bool BattleOnly;
        public readonly Type[] ExtraComponents;

        public ManagerEntry(
            string objectName,
            Type componentType,
            string bootstrapField,
            bool battleOnly = false,
            params Type[] extraComponents)
        {
            ObjectName = objectName;
            ComponentType = componentType;
            BootstrapField = bootstrapField;
            BattleOnly = battleOnly;
            ExtraComponents = extraComponents ?? Array.Empty<Type>();
        }
    }

    /// <summary>菜单：整理当前激活场景的 GameSystems 层级。</summary>
    [MenuItem("Attack Barbarians/Scene/Ensure GameSystems Hierarchy (Current Scene)")]
    public static void EnsureCurrentScene()
    {
        GameObject gameSystems = FindGameSystemsInActiveScene();
        if (gameSystems == null)
        {
            Debug.LogError("[GameSystemsHierarchyEditor] 当前场景未找到 GameSystems。");
            return;
        }

        bool includeBattle = ShouldIncludeBattleManagers(gameSystems);
        Apply(gameSystems, includeBattle);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[GameSystemsHierarchyEditor] 已整理当前场景 GameSystems（战斗 Manager: {includeBattle}）。");
    }

    /// <summary>菜单：打开 MainScene 并整理 GameSystems（不含战斗 Manager）。</summary>
    [MenuItem("Attack Barbarians/Scene/Ensure GameSystems Hierarchy (MainScene)")]
    public static void EnsureMainScene()
    {
        EnsureScene(MainScenePath, includeBattleManagers: false);
    }
    /// <summary>菜单：打开 BattleScene 并整理 GameSystems（含战斗 Manager）。</summary>

    [MenuItem("Attack Barbarians/Scene/Ensure GameSystems Hierarchy (BattleScene)")]
    public static void EnsureBattleScene()
    {
        EnsureScene(BattleScenePath, includeBattleManagers: true);
        if (GameObject.Find("UICanvas") == null)
        {
            BattleSceneUiBuilder.BuildBattleSceneUi();
        }
    }

    /// <summary>菜单：依次整理 MainScene 与 BattleScene。</summary>
    [MenuItem("Attack Barbarians/Scene/Ensure GameSystems Hierarchy (All Scenes)")]
    public static void EnsureAllScenes()
    {
        EnsureScene(MainScenePath, includeBattleManagers: false);
        EnsureScene(BattleScenePath, includeBattleManagers: true);
        Debug.Log("[GameSystemsHierarchyEditor] MainScene 与 BattleScene 均已整理完成。");
    }

    /// <summary>供 <see cref="MainSceneBuilder"/> 在创建主场景时调用。</summary>
    public static void ApplyForMainScene(GameObject gameSystemsRoot)
    {
        Apply(gameSystemsRoot, includeBattleManagers: false);
    }

    /// <summary>供 BattleScene 构建或修复时调用。</summary>
    public static void ApplyForBattleScene(GameObject gameSystemsRoot)
    {
        Apply(gameSystemsRoot, includeBattleManagers: true);
    }

    /// <summary>按 Resolve 顺序创建/排序子 Manager 并绑定 Bootstrapper。</summary>
    /// <param name="gameSystemsRoot">GameSystems 根对象。</param>
    /// <param name="includeBattleManagers">是否包含战斗专用 Manager。</param>
    public static void Apply(GameObject gameSystemsRoot, bool includeBattleManagers)
    {
        if (gameSystemsRoot == null)
        {
            return;
        }

        GameBootstrapper bootstrapper = gameSystemsRoot.GetComponent<GameBootstrapper>();
        if (bootstrapper == null)
        {
            bootstrapper = gameSystemsRoot.AddComponent<GameBootstrapper>();
        }

        IReadOnlyList<ManagerEntry> entries = BuildEntries(includeBattleManagers);
        var orderedTransforms = new List<Transform>(entries.Count);
        var claimed = new HashSet<Transform>();

        for (int i = 0; i < entries.Count; i++)
        {
            ManagerEntry entry = entries[i];
            Transform child = FindOrCreateChild(gameSystemsRoot.transform, entry, claimed);
            orderedTransforms.Add(child);
            claimed.Add(child);
        }

        for (int i = 0; i < orderedTransforms.Count; i++)
        {
            orderedTransforms[i].SetSiblingIndex(i);
        }

        WireBootstrapper(bootstrapper, entries, orderedTransforms);
    }

    /// <summary>打开指定场景并整理 GameSystems 后保存。</summary>
    /// <param name="scenePath">场景路径。</param>
    /// <param name="includeBattleManagers">是否包含战斗 Manager。</param>
    private static void EnsureScene(string scenePath, bool includeBattleManagers)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject gameSystems = FindGameSystemsInScene(scene);
        if (gameSystems == null)
        {
            Debug.LogError($"[GameSystemsHierarchyEditor] {scenePath} 中未找到 GameSystems。");
            return;
        }

        Apply(gameSystems, includeBattleManagers);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[GameSystemsHierarchyEditor] 已保存 {scenePath}（战斗 Manager: {includeBattleManagers}）。");
    }

    /// <summary>在当前激活场景中查找 GameSystems 根节点。</summary>
    private static GameObject FindGameSystemsInActiveScene()
    {
        return FindGameSystemsInScene(SceneManager.GetActiveScene());
    }

    /// <summary>在指定场景中查找 GameSystems 根节点。</summary>
    /// <param name="scene">目标场景。</param>
    private static GameObject FindGameSystemsInScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "GameSystems")
            {
                return root;
            }
        }

        return null;
    }

    /// <summary>根据 Bootstrapper 配置判断是否应挂载战斗 Manager。</summary>
    /// <param name="gameSystems">GameSystems 根对象。</param>
    private static bool ShouldIncludeBattleManagers(GameObject gameSystems)
    {
        GameBootstrapper bootstrapper = gameSystems.GetComponent<GameBootstrapper>();
        if (bootstrapper == null)
        {
            return SceneManager.GetActiveScene().name.Contains("Battle", StringComparison.OrdinalIgnoreCase);
        }

        SerializedObject serialized = new SerializedObject(bootstrapper);
        SerializedProperty managerSetProp = serialized.FindProperty("managerSet");
        SerializedProperty postFlowProp = serialized.FindProperty("postBootstrapFlow");
        if (managerSetProp == null || postFlowProp == null)
        {
            return false;
        }

        BootstrapManagerSet managerSet = (BootstrapManagerSet)managerSetProp.enumValueIndex;
        if (managerSet == BootstrapManagerSet.BattleScene || managerSet == BootstrapManagerSet.All)
        {
            return true;
        }

        if (managerSet == BootstrapManagerSet.MainScene)
        {
            return false;
        }

        BootstrapPostFlow postFlow = (BootstrapPostFlow)postFlowProp.enumValueIndex;
        return postFlow != BootstrapPostFlow.OpenMainMenu;
    }

    /// <summary>构建 Manager 条目列表（主场景或含战斗扩展）。</summary>
    /// <param name="includeBattleManagers">是否包含战斗 Manager。</param>
    private static IReadOnlyList<ManagerEntry> BuildEntries(bool includeBattleManagers)
    {
        var entries = new List<ManagerEntry>(32)
        {
            new("ConfigManager", typeof(ConfigManager), "configManager"),
            new("SaveManager", typeof(SaveManager), "saveManager"),
            new("ResourceManager", typeof(ResourceManager), "resourceManager"),
            new("ShopManager", typeof(ShopManager), "shopManager"),
            new("AdRewardService", typeof(AdRewardService), "adRewardService"),
            new("AchievementManager", typeof(AchievementManager), "achievementManager"),
            new("DailyRewardManager", typeof(DailyRewardManager), "dailyRewardManager"),
            new("UpgradeCardManager", typeof(UpgradeCardManager), "upgradeCardManager"),
            new("BuffUnlockService", typeof(BuffUnlockService), "buffUnlockService"),
            new("MetaRewardService", typeof(MetaRewardService), "metaRewardService"),
            new("TalentManager", typeof(TalentManager), "talentManager"),
            new("EquipmentManager", typeof(EquipmentManager), "equipmentManager"),
            new("PerformanceManager", typeof(PerformanceManager), "performanceManager"),
            new("PoolRoot", typeof(PoolManager), "poolManager"),
            new(
                "GameManager",
                typeof(GameManager),
                "gameManager",
                battleOnly: false,
                includeBattleManagers ? typeof(GameFlowManager) : null),
            new("GameRunSpeedController", typeof(GameRunSpeedController), "gameRunSpeedController"),
            new("SkillUnlockService", typeof(SkillUnlockService), "skillUnlockService"),
            new("ContentRegistry", typeof(ContentRegistry), "contentRegistry"),
            new("AudioManager", typeof(AudioManager), "audioManager"),
        };

        if (!includeBattleManagers)
        {
            return entries;
        }

        entries.AddRange(new[]
        {
            new ManagerEntry("RunSessionTracker", typeof(RunSessionTracker), "runSessionTracker", true),
            new ManagerEntry("RunRewardSettlementService", typeof(RunRewardSettlementService), "runRewardSettlementService", true),
            new ManagerEntry("PlayerExperienceService", typeof(PlayerExperienceService), "playerExperienceService", true),
            new ManagerEntry("UpgradeManager", typeof(UpgradeManager), "upgradeManager", true),
            new ManagerEntry("RandomRewardManager", typeof(RandomRewardManager), "randomRewardManager", true),
            new ManagerEntry("EnemySpawnerManager", typeof(EnemySpawnerManager), "enemySpawnerManager", true),
            new ManagerEntry("WaveManager", typeof(WaveManager), "waveManager", true),
            new ManagerEntry("BossRunStatsBridge", typeof(BossRunStatsBridge), "bossRunStatsBridge", true),
            new ManagerEntry("DamageSystem", typeof(DamageSystem), "damageSystem", true),
            new ManagerEntry("CollisionManager", typeof(CollisionManager), "collisionManager", true),
            new ManagerEntry("ProjectileManager", typeof(ProjectileManager), "projectileManager", true),
            new ManagerEntry("MapManager", typeof(MapManager), "mapManager", true),
            new ManagerEntry("GameplayEventManager", typeof(GameplayEventManager), "gameplayEventManager", true),
            new ManagerEntry("GameplayEventDebugBridge", typeof(GameplayEventDebugBridge), "gameplayEventDebugBridge", true),
        });

        return entries;
    }

    /// <summary>查找或创建 Manager 子物体并挂载组件。</summary>
    /// <param name="root">GameSystems Transform。</param>
    /// <param name="entry">Manager 条目。</param>
    /// <param name="claimed">已占用 Transform 集合。</param>
    private static Transform FindOrCreateChild(
        Transform root,
        ManagerEntry entry,
        HashSet<Transform> claimed)
    {
        Transform existing = FindExistingChild(root, entry.ComponentType, claimed);
        if (existing == null && entry.ExtraComponents.Length > 0)
        {
            for (int i = 0; i < entry.ExtraComponents.Length; i++)
            {
                Type extraType = entry.ExtraComponents[i];
                if (extraType == null)
                {
                    continue;
                }

                existing = FindExistingChild(root, extraType, claimed);
                if (existing != null)
                {
                    break;
                }
            }
        }

        if (existing == null)
        {
            GameObject created = new GameObject(entry.ObjectName);
            Undo.RegisterCreatedObjectUndo(created, "Create GameSystems Child");
            created.transform.SetParent(root, false);
            existing = created.transform;
        }

        if (existing.parent != root)
        {
            existing.SetParent(root, false);
        }

        existing.gameObject.name = entry.ObjectName;
        EnsureComponent(existing.gameObject, entry.ComponentType);

        for (int i = 0; i < entry.ExtraComponents.Length; i++)
        {
            Type extraType = entry.ExtraComponents[i];
            if (extraType != null)
            {
                EnsureComponent(existing.gameObject, extraType);
            }
        }

        return existing;
    }

    /// <summary>在子物体中按组件类型查找未占用的 Transform。</summary>
    /// <param name="root">父 Transform。</param>
    /// <param name="componentType">组件类型。</param>
    /// <param name="claimed">已占用集合。</param>
    private static Transform FindExistingChild(Transform root, Type componentType, HashSet<Transform> claimed)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (claimed.Contains(child))
            {
                continue;
            }

            if (child.GetComponent(componentType) != null)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>确保 GameObject 上存在指定组件。</summary>
    /// <param name="gameObject">目标对象。</param>
    /// <param name="componentType">组件类型。</param>
    private static Component EnsureComponent(GameObject gameObject, Type componentType)
    {
        Component existing = gameObject.GetComponent(componentType);
        if (existing != null)
        {
            return existing;
        }

        return Undo.AddComponent(gameObject, componentType);
    }

    /// <summary>将有序子 Manager 引用写入 GameBootstrapper 序列化字段。</summary>
    /// <param name="bootstrapper">Bootstrapper 组件。</param>
    /// <param name="entries">Manager 条目列表。</param>
    /// <param name="orderedTransforms">排序后的 Transform 列表。</param>
    private static void WireBootstrapper(
        GameBootstrapper bootstrapper,
        IReadOnlyList<ManagerEntry> entries,
        IReadOnlyList<Transform> orderedTransforms)
    {
        SerializedObject serialized = new SerializedObject(bootstrapper);

        for (int i = 0; i < entries.Count; i++)
        {
            ManagerEntry entry = entries[i];
            SerializedProperty property = serialized.FindProperty(entry.BootstrapField);
            if (property == null)
            {
                continue;
            }

            Component component = orderedTransforms[i].GetComponent(entry.ComponentType);
            property.objectReferenceValue = component;
        }

        if (entries.Any(entry => entry.ExtraComponents.Contains(typeof(GameFlowManager))))
        {
            SerializedProperty gameFlowProperty = serialized.FindProperty("gameFlowManager");
            Transform gameManagerTransform = orderedTransforms.FirstOrDefault(
                transform => transform.GetComponent<GameManager>() != null);
            if (gameFlowProperty != null && gameManagerTransform != null)
            {
                gameFlowProperty.objectReferenceValue = gameManagerTransform.GetComponent<GameFlowManager>();
            }
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
