#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 将 Dark Knight 第三方资源转为项目可用的 Boss：构建战斗用 Animator、敌人 Prefab、
/// 基础 <see cref="EnemyDataSO"/> 与 <see cref="BossDataSO"/>，并写入配置总表与对象池。
/// </summary>
public static class DarkKnightBossBootstrapMenu
{
    private const string PackRoot = "Assets/Dark Knight";
    private const string VisualPrefabPath = PackRoot + "/Prefabs/Dark Knight.prefab";
    private const string AnimRoot = PackRoot + "/Animations";

    private const string OutputRoot = "Assets/Prefabs/Enemies/DarkKnight";
    private const string EnemyConfigFolder = "Assets/Resources/Config/Enemy/Boss";
    private const string BossConfigFolder = "Assets/Resources/Config/Boss";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";
    private const string PoolConfigPath = "Assets/Resources/Config/Pool/PoolConfig.asset";

    private const string EnemyConfigId = "enemy.dark_knight";
    private const string BossConfigId = "boss.dark_knight";
    private const string DisplayName = "Dark Knight";
    private const string PoolKey = "Enemy.Boss.DarkKnight";

    private const float VisualScale = 1f;
    private const float AttackDistance = 1.8f;

    [MenuItem("Attack Barbarians/Config/Bootstrap Dark Knight Boss")]
    public static void BootstrapDarkKnightBoss()
    {
        EnsureFolders();

        if (!TryBuildEnemy(out GameObject enemyPrefab, out EnemyDataSO enemyData))
        {
            Debug.LogError("[DarkKnightBoss] Dark Knight Boss 资源构建失败。");
            return;
        }

        BossDataSO bossData = CreateOrUpdateBossData();

        RegisterEnemyInDatabase(enemyData);
        RegisterBossInDatabase(bossData);
        RegisterPoolEntry(enemyPrefab, enemyData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DarkKnightBoss] 已完成 Dark Knight Boss 接入：enemy={EnemyConfigId}，boss={BossConfigId}。", bossData);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(OutputRoot);
        Directory.CreateDirectory($"{OutputRoot}/Animators");
        Directory.CreateDirectory($"{OutputRoot}/Clips");
        Directory.CreateDirectory(EnemyConfigFolder);
        Directory.CreateDirectory(BossConfigFolder);
    }

    private static bool TryBuildEnemy(out GameObject enemyPrefab, out EnemyDataSO enemyData)
    {
        enemyPrefab = null;
        enemyData = null;

        GameObject visualSource = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath);
        if (visualSource == null)
        {
            Debug.LogError($"[DarkKnightBoss] 找不到视觉 Prefab: {VisualPrefabPath}");
            return false;
        }

        AnimationClip walkClip = LoadClip($"{AnimRoot}/Walk.anim");
        AnimationClip attackClip = LoadClip($"{AnimRoot}/Attack.anim");
        AnimationClip deathClip = LoadClip($"{AnimRoot}/Death.anim");
        if (walkClip == null || attackClip == null || deathClip == null)
        {
            Debug.LogError("[DarkKnightBoss] 动画缺失（需要 Walk/Attack/Death）。");
            return false;
        }

        AnimationClip gameplayWalk = SaveGameplayClip(walkClip, $"{OutputRoot}/Clips/Walk_Gameplay.anim", addAttackEvents: false);
        AnimationClip gameplayAttack = SaveGameplayClip(attackClip, $"{OutputRoot}/Clips/Attack_Gameplay.anim", addAttackEvents: true);
        AnimationClip gameplayDeath = SaveGameplayClip(deathClip, $"{OutputRoot}/Clips/Death_Gameplay.anim", addAttackEvents: false, deathOnly: true);

        AnimatorController controller = BuildGameplayController(gameplayWalk, gameplayAttack, gameplayDeath);
        if (controller == null)
        {
            return false;
        }

        string prefabPath = $"{OutputRoot}/EnemyBoss_DarkKnight.prefab";
        enemyPrefab = BuildEnemyPrefab(visualSource, controller, prefabPath);
        if (enemyPrefab == null)
        {
            return false;
        }

        enemyData = CreateOrUpdateEnemyData(enemyPrefab);
        return enemyData != null;
    }

    private static AnimationClip LoadClip(string path) =>
        AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

    private static AnimationClip SaveGameplayClip(
        AnimationClip source,
        string destPath,
        bool addAttackEvents,
        bool deathOnly = false)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath) != null)
        {
            AssetDatabase.DeleteAsset(destPath);
        }

        AnimationClip clip = Object.Instantiate(source);
        clip.name = Path.GetFileNameWithoutExtension(destPath);

        float length = Mathf.Max(clip.length, 0.05f);
        var events = new List<AnimationEvent>();
        if (addAttackEvents)
        {
            events.Add(new AnimationEvent { time = length * 0.4f, functionName = "OnAttackTrigger" });
            events.Add(new AnimationEvent { time = length * 0.95f, functionName = "OnAnimationFinished" });
        }
        else if (deathOnly)
        {
            events.Add(new AnimationEvent { time = length * 0.95f, functionName = "OnAnimationFinished" });
        }

        AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        AssetDatabase.CreateAsset(clip, destPath);
        return clip;
    }

    private static AnimatorController BuildGameplayController(
        AnimationClip walk,
        AnimationClip attack,
        AnimationClip death)
    {
        string controllerPath = $"{OutputRoot}/Animators/DarkKnightGameplay.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter(EntityAnimParams.EnemyMove, AnimatorControllerParameterType.Bool);
        controller.AddParameter(EntityAnimParams.EnemyAttack, AnimatorControllerParameterType.Bool);
        controller.AddParameter(EntityAnimParams.EnemyDead, AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState moveState = root.AddState("Move", new Vector3(250f, 130f, 0f));
        moveState.motion = walk;
        AnimatorState attackState = root.AddState("Attack", new Vector3(250f, 40f, 0f));
        attackState.motion = attack;
        AnimatorState deadState = root.AddState("Dead", new Vector3(250f, 230f, 0f));
        deadState.motion = death;

        root.defaultState = moveState;

        AnimatorStateTransition moveExit = moveState.AddExitTransition();
        moveExit.AddCondition(AnimatorConditionMode.IfNot, 0f, EntityAnimParams.EnemyMove);
        moveExit.hasExitTime = false;
        moveExit.duration = 0f;

        AnimatorStateTransition attackExit = attackState.AddExitTransition();
        attackExit.AddCondition(AnimatorConditionMode.IfNot, 0f, EntityAnimParams.EnemyAttack);
        attackExit.hasExitTime = false;
        attackExit.duration = 0f;

        AnimatorStateTransition deadExit = deadState.AddExitTransition();
        deadExit.AddCondition(AnimatorConditionMode.IfNot, 0f, EntityAnimParams.EnemyDead);
        deadExit.hasExitTime = false;
        deadExit.duration = 0f;

        AnimatorTransition toAttack = root.AddEntryTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, EntityAnimParams.EnemyAttack);

        AnimatorTransition toDead = root.AddEntryTransition(deadState);
        toDead.AddCondition(AnimatorConditionMode.If, 0f, EntityAnimParams.EnemyDead);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject BuildEnemyPrefab(
        GameObject visualSource,
        AnimatorController controller,
        string prefabPath)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");

        GameObject root = new GameObject("EnemyBoss_DarkKnight");
        root.layer = enemyLayer;
        root.tag = "Enemy";

        BatEnemy batEnemy = root.AddComponent<BatEnemy>();
        root.AddComponent<Entity_Stats>();
        root.AddComponent<Enemy_Health>();
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        EnemyController enemyController = root.AddComponent<EnemyController>();
        root.AddComponent<EnemyStatusController>();
        root.AddComponent<BossController>();

        SerializedObject enemySo = new SerializedObject(batEnemy);
        enemySo.FindProperty("attackCheck").objectReferenceValue = root.transform;
        enemySo.FindProperty("attackDistance").floatValue = AttackDistance;
        enemySo.FindProperty("wallLayer").intValue = wallLayerMask;
        enemySo.FindProperty("moveSpeed").floatValue = 1.5f;
        enemySo.FindProperty("cooldownThreshold").floatValue = 1.5f;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSo = new SerializedObject(enemyController);
        controllerSo.FindProperty("defaultConfigId").stringValue = EnemyConfigId;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualSource, root.transform);
        PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * VisualScale;
        SetLayerRecursively(visual, enemyLayer);
        StripThirdPartyComponents(visual);
        ApplyEnemySorting(visual);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("[DarkKnightBoss] 视觉 Prefab 缺少 Animator。");
            return null;
        }

        animator.runtimeAnimatorController = controller;
        if (animator.gameObject.GetComponent<EnemyAnimatorTrigger>() == null)
        {
            animator.gameObject.AddComponent<EnemyAnimatorTrigger>();
        }

        Bounds bounds = CalculateLocalBounds(visual.transform);
        collider.offset = bounds.center;
        collider.size = new Vector2(
            Mathf.Max(0.4f, bounds.size.x),
            Mathf.Max(0.4f, bounds.size.y));

        GameObject prefab = SavePrefab(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    /// <summary>移除 Dark Knight 自带的运行时脚本与物理组件，避免与项目敌人系统冲突。</summary>
    private static void StripThirdPartyComponents(GameObject visual)
    {
        var behaviours = visual.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName == "DarkKnightController" || typeName == "BeamShot")
            {
                Object.DestroyImmediate(behaviour);
            }
        }

        foreach (Rigidbody2D body in visual.GetComponentsInChildren<Rigidbody2D>(true))
        {
            Object.DestroyImmediate(body);
        }

        foreach (Collider2D col in visual.GetComponentsInChildren<Collider2D>(true))
        {
            Object.DestroyImmediate(col);
        }
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return PrefabUtility.SaveAsPrefabAsset(root, path);
    }

    private static EnemyDataSO CreateOrUpdateEnemyData(GameObject prefab)
    {
        string assetPath = $"{EnemyConfigFolder}/EnemyData_DarkKnight.asset";
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = EnemyConfigId;
        so.FindProperty("displayName").stringValue = DisplayName;
        SerializedProperty stats = so.FindProperty("baseStats");
        stats.FindPropertyRelative("maxHp").floatValue = 2000f;
        stats.FindPropertyRelative("moveSpeed").floatValue = 1.5f;
        stats.FindPropertyRelative("attackSpeed").floatValue = 1f;
        stats.FindPropertyRelative("attackSpeedMulti").floatValue = 1f;
        stats.FindPropertyRelative("damage").floatValue = 40f;
        so.FindProperty("attackDistance").floatValue = AttackDistance;
        so.FindProperty("contactDamage").floatValue = 40f;
        so.FindProperty("attackCooldown").floatValue = 1.5f;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("poolKey").stringValue = PoolKey;
        so.FindProperty("spawnWeight").intValue = 1;
        so.FindProperty("abilityTags").enumValueIndex = (int)EnemyAbilityTag.Normal;
        so.FindProperty("experienceReward").intValue = 100;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static BossDataSO CreateOrUpdateBossData()
    {
        string assetPath = $"{BossConfigFolder}/BossData_DarkKnight.asset";
        BossDataSO data = AssetDatabase.LoadAssetAtPath<BossDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BossDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = BossConfigId;
        so.FindProperty("displayName").stringValue = "Dark Knight";
        so.FindProperty("baseEnemyConfigId").stringValue = EnemyConfigId;

        SerializedProperty overrides = so.FindProperty("statOverrides");
        overrides.FindPropertyRelative("maxHp").floatValue = 2000f;
        overrides.FindPropertyRelative("moveSpeed").floatValue = 1.5f;
        overrides.FindPropertyRelative("attackSpeed").floatValue = 1f;
        overrides.FindPropertyRelative("attackSpeedMulti").floatValue = 1f;
        overrides.FindPropertyRelative("damage").floatValue = 40f;
        overrides.FindPropertyRelative("critPower").floatValue = 0.5f;
        so.FindProperty("useStatOverrides").boolValue = true;

        so.FindProperty("phaseCount").intValue = 2;
        so.FindProperty("phaseTransitionMode").enumValueIndex = (int)BossPhaseTransitionMode.HealthRatio;
        SetFloatList(so.FindProperty("phaseHpThresholds"), new[] { 0.5f });

        so.FindProperty("dropTableId").stringValue = GameConstants.ConfigIds.DropTableCommon;
        so.FindProperty("bonusExperience").intValue = 200;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void SetFloatList(SerializedProperty list, IReadOnlyList<float> values)
    {
        if (list == null)
        {
            return;
        }

        list.ClearArray();
        for (int i = 0; i < values.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).floatValue = values[i];
        }
    }

    private static void RegisterEnemyInDatabase(EnemyDataSO enemy)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[DarkKnightBoss] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        AddUnique(so.FindProperty("enemies"), enemy);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void RegisterBossInDatabase(BossDataSO boss)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(database);
        AddUnique(so.FindProperty("bosses"), boss);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void RegisterPoolEntry(GameObject prefab, EnemyDataSO enemyData)
    {
        PoolConfigSO poolConfig = AssetDatabase.LoadAssetAtPath<PoolConfigSO>(PoolConfigPath);
        if (poolConfig == null)
        {
            Debug.LogWarning("[DarkKnightBoss] 未找到 PoolConfig.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(poolConfig);
        SerializedProperty entries = so.FindProperty("entries");
        string key = enemyData.PoolKey;

        int index = FindPoolEntryIndex(entries, key);
        if (index < 0)
        {
            index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
        }

        SerializedProperty entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("key").stringValue = key;
        entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("initialCount").intValue = 1;
        entry.FindPropertyRelative("maxCount").intValue = 4;
        entry.FindPropertyRelative("canExpand").boolValue = true;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(poolConfig);
    }

    private static int FindPoolEntryIndex(SerializedProperty entries, string key)
    {
        for (int i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue == key)
            {
                return i;
            }
        }

        return -1;
    }

    private static void AddUnique(SerializedProperty list, Object item)
    {
        if (list == null || item == null)
        {
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return;
            }
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).objectReferenceValue = item;
    }

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        Transform transform = go.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            SetLayerRecursively(transform.GetChild(i).gameObject, layer);
        }
    }

    private static void ApplyEnemySorting(GameObject visual)
    {
        int sortingLayerId = SortingLayer.NameToID("Enemy");
        SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerID = sortingLayerId;
        }
    }

    private static Bounds CalculateLocalBounds(Transform visualRoot)
    {
        Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, new Vector3(1f, 1f, 0.1f));
        }

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = visualRoot.parent != null
            ? visualRoot.parent.InverseTransformPoint(worldBounds.center)
            : worldBounds.center;
        Vector3 localSize = worldBounds.size;
        if (visualRoot.parent != null)
        {
            Vector3 lossy = visualRoot.parent.lossyScale;
            localSize = new Vector3(
                localSize.x / Mathf.Max(0.0001f, lossy.x),
                localSize.y / Mathf.Max(0.0001f, lossy.y),
                localSize.z);
        }

        return new Bounds(localCenter, localSize);
    }
}
#endif
