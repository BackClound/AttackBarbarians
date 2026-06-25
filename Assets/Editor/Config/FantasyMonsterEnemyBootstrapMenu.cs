#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 将 2D Fantasy Monster Pack 1 的展示 Prefab 转为可刷怪敌人，并写入配置表、对象池与波次池。
/// </summary>
public static class FantasyMonsterEnemyBootstrapMenu
{
    private const string PackRoot = "Assets/ImageResources/2D Fantasy Monster Pack 1";
    private const string OutputRoot = "Assets/Prefabs/Enemies/FantasyMonster";
    private const string ConfigFolder = "Assets/Resources/Config/Enemy/Fantasy";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";
    private const string PoolConfigPath = "Assets/Resources/Config/Pool/PoolConfig.asset";
    private const string Wave01Path = "Assets/Resources/Config/Wave/WaveData_01.asset";

    private static readonly MonsterDef[] Monsters =
    {
        new("bat", "Bat", "Bat03", "enemy.fantasy.bat", "Fantasy Bat", 30, 2.5f, 10f, 0.35f),
        new("boar", "Boar", "Boar04", "enemy.fantasy.boar", "Fantasy Boar", 45, 1.8f, 12f, 0.35f),
        new("wolf", "Wolf", "Wolf03", "enemy.fantasy.wolf", "Fantasy Wolf", 35, 3.0f, 11f, 0.35f),
        new("slime", "Slime", "Slime04", "enemy.fantasy.slime", "Fantasy Slime", 25, 1.5f, 8f, 0.4f),
        new("shroom", "Shroom", "Shroom01", "enemy.fantasy.shroom", "Fantasy Shroom", 28, 1.6f, 9f, 0.4f),
        new("skeleton", "Skeleton", "Skeleton04", "enemy.fantasy.skeleton", "Fantasy Skeleton", 40, 2.0f, 12f, 0.32f),
        new("golem", "Golem", "Golem03", "enemy.fantasy.golem", "Fantasy Golem", 80, 1.2f, 18f, 0.28f),
        new("troll", "Troll", "Troll 04", "enemy.fantasy.troll", "Fantasy Troll", 70, 1.4f, 16f, 0.3f),
    };

    [MenuItem("Attack Barbarians/Config/Bootstrap Fantasy Monster Enemies")]
    public static void BootstrapFantasyMonsterEnemies()
    {
        EnsureFolders();

        var enemyAssets = new List<EnemyDataSO>(Monsters.Length);
        var poolPrefabs = new List<GameObject>(Monsters.Length);

        for (int i = 0; i < Monsters.Length; i++)
        {
            MonsterDef def = Monsters[i];
            if (!TryBuildEnemy(def, out GameObject enemyPrefab, out EnemyDataSO enemyData))
            {
                Debug.LogError($"[FantasyMonsterBootstrap] 跳过 {def.DisplayName}，资源构建失败。");
                continue;
            }

            enemyAssets.Add(enemyData);
            poolPrefabs.Add(enemyPrefab);
        }

        RegisterEnemiesInDatabase(enemyAssets);
        RegisterPoolEntries(poolPrefabs, enemyAssets);
        AppendToWavePool(enemyAssets);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FantasyMonsterBootstrap] 已完成 {enemyAssets.Count} 个 Fantasy Monster 敌人接入。");
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(OutputRoot);
        Directory.CreateDirectory($"{OutputRoot}/Animators");
        Directory.CreateDirectory($"{OutputRoot}/Clips");
        Directory.CreateDirectory(ConfigFolder);
    }

    private static bool TryBuildEnemy(MonsterDef def, out GameObject enemyPrefab, out EnemyDataSO enemyData)
    {
        enemyPrefab = null;
        enemyData = null;

        string visualPath = $"{PackRoot}/Prefabs/{def.Folder}/{def.VisualPrefabName}.prefab";
        GameObject visualSource = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
        if (visualSource == null)
        {
            Debug.LogError($"[FantasyMonsterBootstrap] 找不到视觉 Prefab: {visualPath}");
            return false;
        }

        string animFolder = $"{PackRoot}/Animation/{def.Folder}";
        AnimationClip walkClip = LoadClip($"{animFolder}/Walk.anim");
        AnimationClip attackClip = LoadClip($"{animFolder}/Attack.anim");
        AnimationClip deathClip = LoadClip($"{animFolder}/Death.anim");
        if (walkClip == null || attackClip == null || deathClip == null)
        {
            Debug.LogError($"[FantasyMonsterBootstrap] {def.Folder} 动画缺失（需要 Walk/Attack/Death）。");
            return false;
        }

        string clipFolder = $"{OutputRoot}/Clips/{def.Folder}";
        Directory.CreateDirectory(clipFolder);

        AnimationClip gameplayWalk = SaveGameplayClip(walkClip, $"{clipFolder}/Walk_Gameplay.anim", addAttackEvents: false);
        AnimationClip gameplayAttack = SaveGameplayClip(attackClip, $"{clipFolder}/Attack_Gameplay.anim", addAttackEvents: true);
        AnimationClip gameplayDeath = SaveGameplayClip(deathClip, $"{clipFolder}/Death_Gameplay.anim", addAttackEvents: false, deathOnly: true);

        AnimatorController controller = BuildGameplayController(def, gameplayWalk, gameplayAttack, gameplayDeath);
        if (controller == null)
        {
            return false;
        }

        string prefabPath = $"{OutputRoot}/EnemyFantasy_{def.Folder}.prefab";
        enemyPrefab = BuildEnemyPrefab(def, visualSource, controller, prefabPath);
        if (enemyPrefab == null)
        {
            return false;
        }

        enemyData = CreateOrUpdateEnemyData(def, enemyPrefab, prefabPath);
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
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(destPath);
        }

        AnimationClip clip = Object.Instantiate(source);
        clip.name = Path.GetFileNameWithoutExtension(destPath);

        float length = Mathf.Max(clip.length, 0.05f);
        var events = new List<AnimationEvent>();
        if (addAttackEvents)
        {
            events.Add(new AnimationEvent
            {
                time = length * 0.4f,
                functionName = "OnAttackTrigger",
            });
            events.Add(new AnimationEvent
            {
                time = length * 0.95f,
                functionName = "OnAnimationFinished",
            });
        }
        else if (deathOnly)
        {
            events.Add(new AnimationEvent
            {
                time = length * 0.95f,
                functionName = "OnAnimationFinished",
            });
        }

        AnimationUtility.SetAnimationEvents(clip, events.ToArray());
        AssetDatabase.CreateAsset(clip, destPath);
        return clip;
    }

    private static AnimatorController BuildGameplayController(
        MonsterDef def,
        AnimationClip walk,
        AnimationClip attack,
        AnimationClip death)
    {
        string controllerPath = $"{OutputRoot}/Animators/{def.Folder}Gameplay.controller";
        AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (existing != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("isMove", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isAttack", AnimatorControllerParameterType.Bool);
        controller.AddParameter("isDead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine root = controller.layers[0].stateMachine;
        AnimatorState moveState = root.AddState("Move", new Vector3(250f, 130f, 0f));
        moveState.motion = walk;
        AnimatorState attackState = root.AddState("Attack", new Vector3(250f, 40f, 0f));
        attackState.motion = attack;
        AnimatorState deadState = root.AddState("Dead", new Vector3(250f, 230f, 0f));
        deadState.motion = death;

        root.defaultState = moveState;

        AnimatorStateTransition moveExit = moveState.AddExitTransition();
        moveExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "isMove");
        moveExit.hasExitTime = false;
        moveExit.duration = 0f;

        AnimatorStateTransition attackExit = attackState.AddExitTransition();
        attackExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "isAttack");
        attackExit.hasExitTime = false;
        attackExit.duration = 0f;

        AnimatorStateTransition deadExit = deadState.AddExitTransition();
        deadExit.AddCondition(AnimatorConditionMode.IfNot, 0f, "isDead");
        deadExit.hasExitTime = false;
        deadExit.duration = 0f;

        AnimatorTransition toAttack = root.AddEntryTransition(attackState);
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, "isAttack");

        AnimatorTransition toDead = root.AddEntryTransition(deadState);
        toDead.AddCondition(AnimatorConditionMode.If, 0f, "isDead");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static GameObject BuildEnemyPrefab(
        MonsterDef def,
        GameObject visualSource,
        AnimatorController controller,
        string prefabPath)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");

        GameObject root = new GameObject($"EnemyFantasy_{def.Folder}");
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

        SerializedObject enemySo = new SerializedObject(batEnemy);
        enemySo.FindProperty("attackCheck").objectReferenceValue = root.transform;
        enemySo.FindProperty("attackDistance").floatValue = 1.5f;
        enemySo.FindProperty("wallLayer").intValue = wallLayerMask;
        enemySo.FindProperty("moveSpeed").floatValue = def.MoveSpeed;
        enemySo.FindProperty("cooldownThreshold").floatValue = 1f;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSo = new SerializedObject(enemyController);
        controllerSo.FindProperty("defaultConfigId").stringValue = def.ConfigId;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualSource, root.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * def.VisualScale;
        SetLayerRecursively(visual, enemyLayer);
        ApplyEnemySorting(visual);

        Animator animator = visual.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Object.DestroyImmediate(root);
            Debug.LogError($"[FantasyMonsterBootstrap] {def.Folder} 视觉 Prefab 缺少 Animator。");
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

    private static GameObject SavePrefab(GameObject root, string path)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, path, InteractionMode.AutomatedAction);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        return PrefabUtility.SaveAsPrefabAsset(root, path);
    }

    private static EnemyDataSO CreateOrUpdateEnemyData(MonsterDef def, GameObject prefab, string prefabPath)
    {
        string assetPath = $"{ConfigFolder}/EnemyData_{def.Folder}.asset";
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = def.ConfigId;
        so.FindProperty("displayName").stringValue = def.DisplayName;
        so.FindProperty("baseStats").FindPropertyRelative("maxHp").floatValue = def.MaxHp;
        so.FindProperty("baseStats").FindPropertyRelative("moveSpeed").floatValue = def.MoveSpeed;
        so.FindProperty("baseStats").FindPropertyRelative("attackSpeed").floatValue = 1f;
        so.FindProperty("baseStats").FindPropertyRelative("attackSpeedMulti").floatValue = 1f;
        so.FindProperty("baseStats").FindPropertyRelative("damage").floatValue = def.Damage;
        so.FindProperty("attackDistance").floatValue = 1.5f;
        so.FindProperty("contactDamage").floatValue = def.Damage;
        so.FindProperty("attackCooldown").floatValue = 1f;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("poolKey").stringValue = def.PoolKey;
        so.FindProperty("spawnWeight").intValue = 1;
        so.FindProperty("abilityTags").enumValueIndex = (int)EnemyAbilityTag.Normal;
        so.FindProperty("experienceReward").intValue = 5;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void RegisterEnemiesInDatabase(IReadOnlyList<EnemyDataSO> enemies)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[FantasyMonsterBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("enemies");
        for (int i = 0; i < enemies.Count; i++)
        {
            AddUnique(list, enemies[i]);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void RegisterPoolEntries(IReadOnlyList<GameObject> prefabs, IReadOnlyList<EnemyDataSO> enemyDataList)
    {
        PoolConfigSO poolConfig = AssetDatabase.LoadAssetAtPath<PoolConfigSO>(PoolConfigPath);
        if (poolConfig == null)
        {
            Debug.LogWarning("[FantasyMonsterBootstrap] 未找到 PoolConfig.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(poolConfig);
        SerializedProperty entries = so.FindProperty("entries");
        for (int i = 0; i < prefabs.Count && i < enemyDataList.Count; i++)
        {
            string key = enemyDataList[i].PoolKey;
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            int index = FindPoolEntryIndex(entries, key);
            if (index < 0)
            {
                index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("key").stringValue = key;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            entry.FindPropertyRelative("initialCount").intValue = 4;
            entry.FindPropertyRelative("maxCount").intValue = 24;
            entry.FindPropertyRelative("canExpand").boolValue = true;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(poolConfig);
    }

    private static void AppendToWavePool(IReadOnlyList<EnemyDataSO> enemies)
    {
        WaveDataSO wave = AssetDatabase.LoadAssetAtPath<WaveDataSO>(Wave01Path);
        if (wave == null)
        {
            Debug.LogWarning("[FantasyMonsterBootstrap] 未找到 WaveData_01.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(wave);
        SerializedProperty ids = so.FindProperty("enemyConfigIds");
        for (int i = 0; i < enemies.Count; i++)
        {
            AddUniqueString(ids, enemies[i].ConfigId);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(wave);
    }

    private static int FindPoolEntryIndex(SerializedProperty entries, string key)
    {
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("key").stringValue == key)
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

    private static void AddUniqueString(SerializedProperty list, string value)
    {
        if (list == null || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).stringValue == value)
            {
                return;
            }
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).stringValue = value;
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

    private readonly struct MonsterDef
    {
        public MonsterDef(
            string id,
            string folder,
            string visualPrefabName,
            string configId,
            string displayName,
            int maxHp,
            float moveSpeed,
            float damage,
            float visualScale)
        {
            Id = id;
            Folder = folder;
            VisualPrefabName = visualPrefabName;
            ConfigId = configId;
            DisplayName = displayName;
            MaxHp = maxHp;
            MoveSpeed = moveSpeed;
            Damage = damage;
            VisualScale = visualScale;
        }

        public string Id { get; }
        public string Folder { get; }
        public string VisualPrefabName { get; }
        public string ConfigId { get; }
        public string DisplayName { get; }
        public int MaxHp { get; }
        public float MoveSpeed { get; }
        public float Damage { get; }
        public float VisualScale { get; }
        public string PoolKey => $"Enemy.Fantasy.{Folder}";
    }
}
#endif
