#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Azure Will-o'-Wisp（Spine）资源转为项目 Boss：构建敌人 Prefab、
/// <see cref="EnemyDataSO"/> 与 <see cref="BossDataSO"/>，并写入配置总表与对象池。
/// </summary>
public static class WillOWispBossBootstrapMenu
{
    private const string PackRoot = "Assets/Azure Will-o\u2019-Wisp";
    private const string VisualPrefabPath = PackRoot + "/Prefab/Spine GameObject (Azure Will-o\u2019-Wisp).prefab";
    private const string IconPath = PackRoot + "/Azure Will-o\u2019-Wisp.png";

    private const string OutputRoot = "Assets/Prefabs/Enemies/WillOWisp";
    private const string EnemyConfigFolder = "Assets/Resources/Config/Enemy/Boss";
    private const string BossConfigFolder = "Assets/Resources/Config/Boss";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";
    private const string PoolConfigPath = "Assets/Resources/Config/Pool/PoolConfig.asset";

    private const string EnemyConfigId = "enemy.will_o_wisp";
    private const string BossConfigId = "boss.will_o_wisp";
    private const string DisplayName = "Azure Will-o'-Wisp";
    private const string PoolKey = "Enemy.Boss.WillOWisp";

    private const float TargetVisualHeight = 1.6f;
    private const float AttackDistance = 1.6f;

    [MenuItem("Attack Barbarians/Config/Bootstrap Will-o'-Wisp Boss")]
    public static void BootstrapWillOWispBoss()
    {
        EnsureFolders();

        if (!TryBuildEnemy(out GameObject enemyPrefab, out EnemyDataSO enemyData))
        {
            Debug.LogError("[WillOWispBoss] Will-o'-Wisp Boss 资源构建失败。");
            return;
        }

        BossDataSO bossData = CreateOrUpdateBossData();

        RegisterEnemyInDatabase(enemyData);
        RegisterBossInDatabase(bossData);
        RegisterPoolEntry(enemyPrefab, enemyData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[WillOWispBoss] 已完成 Will-o'-Wisp Boss 接入：enemy={EnemyConfigId}，boss={BossConfigId}。", bossData);
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(OutputRoot);
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
            Debug.LogError($"[WillOWispBoss] 找不到视觉 Prefab: {VisualPrefabPath}");
            return false;
        }

        string prefabPath = $"{OutputRoot}/EnemyBoss_WillOWisp.prefab";
        enemyPrefab = BuildEnemyPrefab(visualSource, prefabPath);
        if (enemyPrefab == null)
        {
            return false;
        }

        enemyData = CreateOrUpdateEnemyData(enemyPrefab);
        return enemyData != null;
    }

    private static GameObject BuildEnemyPrefab(GameObject visualSource, string prefabPath)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        int wallLayerMask = 1 << LayerMask.NameToLayer("Wall");

        GameObject root = new GameObject("EnemyBoss_WillOWisp");
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
        enemySo.FindProperty("moveSpeed").floatValue = 2f;
        enemySo.FindProperty("cooldownThreshold").floatValue = 1.2f;
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject controllerSo = new SerializedObject(enemyController);
        controllerSo.FindProperty("defaultConfigId").stringValue = EnemyConfigId;
        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualSource, root.transform);
        PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        SetLayerRecursively(visual, enemyLayer);
        ApplyEnemySorting(visual);

        if (visual.GetComponent<SpineEnemyAnimationBridge>() == null)
        {
            visual.AddComponent<SpineEnemyAnimationBridge>();
        }

        FitVisualScale(visual.transform);
        ApplyVisualOffset(visual.transform);

        Bounds bounds = CalculateLocalBounds(visual.transform);
        collider.offset = bounds.center;
        collider.size = new Vector2(
            Mathf.Max(0.4f, bounds.size.x),
            Mathf.Max(0.4f, bounds.size.y));

        GameObject prefab = SavePrefab(root, prefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void FitVisualScale(Transform visualRoot)
    {
        Bounds bounds = CalculateLocalBounds(visualRoot);
        float height = Mathf.Max(0.01f, bounds.size.y);
        float scale = TargetVisualHeight / height;
        visualRoot.localScale = Vector3.one * scale;
    }

    private static void ApplyVisualOffset(Transform visualRoot)
    {
        Bounds bounds = CalculateLocalBounds(visualRoot);
        visualRoot.localPosition = new Vector3(0f, -bounds.center.y, 0f);
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
        string assetPath = $"{EnemyConfigFolder}/EnemyData_WillOWisp.asset";
        EnemyDataSO data = AssetDatabase.LoadAssetAtPath<EnemyDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = EnemyConfigId;
        so.FindProperty("displayName").stringValue = DisplayName;
        so.FindProperty("icon").objectReferenceValue = icon;
        SerializedProperty stats = so.FindProperty("baseStats");
        stats.FindPropertyRelative("maxHp").floatValue = 1500f;
        stats.FindPropertyRelative("moveSpeed").floatValue = 2f;
        stats.FindPropertyRelative("attackSpeed").floatValue = 1f;
        stats.FindPropertyRelative("attackSpeedMulti").floatValue = 1f;
        stats.FindPropertyRelative("damage").floatValue = 35f;
        so.FindProperty("attackDistance").floatValue = AttackDistance;
        so.FindProperty("contactDamage").floatValue = 35f;
        so.FindProperty("attackCooldown").floatValue = 1.2f;
        so.FindProperty("prefab").objectReferenceValue = prefab;
        so.FindProperty("poolKey").stringValue = PoolKey;
        so.FindProperty("spawnWeight").intValue = 1;
        so.FindProperty("abilityTags").enumValueIndex = (int)EnemyAbilityTag.Normal;
        so.FindProperty("experienceReward").intValue = 80;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static BossDataSO CreateOrUpdateBossData()
    {
        string assetPath = $"{BossConfigFolder}/BossData_WillOWisp.asset";
        BossDataSO data = AssetDatabase.LoadAssetAtPath<BossDataSO>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<BossDataSO>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = BossConfigId;
        so.FindProperty("displayName").stringValue = DisplayName;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("baseEnemyConfigId").stringValue = EnemyConfigId;

        SerializedProperty overrides = so.FindProperty("statOverrides");
        overrides.FindPropertyRelative("maxHp").floatValue = 1500f;
        overrides.FindPropertyRelative("moveSpeed").floatValue = 2f;
        overrides.FindPropertyRelative("attackSpeed").floatValue = 1f;
        overrides.FindPropertyRelative("attackSpeedMulti").floatValue = 1f;
        overrides.FindPropertyRelative("damage").floatValue = 35f;
        overrides.FindPropertyRelative("critPower").floatValue = 0.5f;
        so.FindProperty("useStatOverrides").boolValue = true;

        so.FindProperty("phaseCount").intValue = 2;
        so.FindProperty("phaseTransitionMode").enumValueIndex = (int)BossPhaseTransitionMode.HealthRatio;
        SetFloatList(so.FindProperty("phaseHpThresholds"), new[] { 0.5f });

        so.FindProperty("dropTableId").stringValue = GameConstants.ConfigIds.DropTableCommon;
        so.FindProperty("bonusExperience").intValue = 150;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static void SetFloatList(SerializedProperty list, float[] values)
    {
        if (list == null)
        {
            return;
        }

        list.ClearArray();
        for (int i = 0; i < values.Length; i++)
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
            Debug.LogWarning("[WillOWispBoss] 未找到 ConfigDatabase.asset。");
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
            Debug.LogWarning("[WillOWispBoss] 未找到 PoolConfig.asset。");
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
        MeshRenderer[] meshRenderers = visual.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            meshRenderers[i].sortingLayerID = sortingLayerId;
        }

        SpriteRenderer[] spriteRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].sortingLayerID = sortingLayerId;
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
