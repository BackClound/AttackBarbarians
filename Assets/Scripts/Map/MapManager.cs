using UnityEngine;

/// <summary>
/// 地图管理器：加载地图配置、设置相机边界、初始化生成区域与背景表现。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上，须在 <see cref="WaveManager"/> 之前初始化。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;MapManager&gt;()</c>。</para>
/// </remarks>
public class MapManager : MonoBehaviour, IGameSystem
{
    [Header("Map")]
    [SerializeField] private string defaultMapConfigId = GameConstants.ConfigIds.MapDefault;
    [SerializeField] private bool loadDefaultMapOnInitialize = true;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SpawnAreaController spawnArea;
    [SerializeField] private Transform backgroundRoot;
    [SerializeField] private bool useAddressablesLoader;

    private ContentRegistry contentRegistry;
    private IContentAssetLoader assetLoader;
    private MapDataSO currentMap;
    private GameObject activeBackgroundInstance;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>是否已成功加载地图配置。</summary>
    public bool IsMapLoaded => currentMap != null;
    /// <summary>当前加载的地图配置。</summary>
    public MapDataSO CurrentMap => currentMap;

    /// <summary>
    /// 解析引用、加载默认地图并完成初始化。
    /// </summary>
    public void Initialize()
    {
        contentRegistry = ServiceLocator.TryGet(out ContentRegistry registry) ? registry : null;
        assetLoader = useAddressablesLoader
            ? new AddressablesContentAssetLoaderStub()
            : new ResourcesContentAssetLoader();

        ResolveReferences();

        string mapId = defaultMapConfigId;
        if (ServiceLocator.TryGet(out ConfigManager configManager) &&
            configManager.GameConfig != null &&
            !string.IsNullOrWhiteSpace(configManager.GameConfig.DefaultMapConfigId))
        {
            mapId = configManager.GameConfig.DefaultMapConfigId;
        }

        if (loadDefaultMapOnInitialize)
        {
            if (!TryLoadMap(mapId))
            {
                Debug.LogWarning(
                    $"[MapManager] 默认地图 configId={mapId} 未找到，将使用场景内 SpawnArea 默认值。");
                ApplyFallbackSpawnArea();
            }
        }

        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭管理器，清理背景实例并重置运行时上下文。
    /// </summary>
    public void Shutdown()
    {
        ClearBackgroundInstance();
        MapRuntimeContext.Reset();
        currentMap = null;
        isInitialized = false;
    }

    /// <summary>
    /// 按 configId 加载并应用地图配置。
    /// </summary>
    /// <param name="mapConfigId">地图配置 Id。</param>
    /// <returns>加载成功返回 true，否则返回 false。</returns>
    public bool TryLoadMap(string mapConfigId)
    {
        if (contentRegistry == null || string.IsNullOrWhiteSpace(mapConfigId))
        {
            Debug.LogError("[MapManager] 无法加载地图：ContentRegistry 或 mapConfigId 无效。");
            return false;
        }

        if (!contentRegistry.TryGetMap(mapConfigId, out MapDataSO mapData))
        {
            Debug.LogError($"[MapManager] 未找到地图配置 configId={mapConfigId}");
            return false;
        }

        ApplyMap(mapData);
        return true;
    }

    /// <summary>将地图配置应用到相机、生成区域、墙体与背景。</summary>
    /// <param name="mapData">待应用的地图配置。</param>
    private void ApplyMap(MapDataSO mapData)
    {
        currentMap = mapData;
        MapRuntimeContext.SetMap(mapData);

        ApplyCameraBounds(mapData.CameraBounds);
        ApplySpawnArea(mapData.SpawnArea);
        ApplyWallPlacement(mapData.WallPlacement);
        ApplyBackground(mapData);

        if (!string.IsNullOrWhiteSpace(mapData.BgmAudioId))
        {
            GameEvents.RaiseAudioPlayMusic(this, mapData.BgmAudioId);
        }

        GameEvents.RaiseMapLoaded(this, new MapLoadedEventArgs(mapData.ConfigId, mapData.RecommendedDifficulty));
    }

    /// <summary>解析主相机、生成区域与背景根节点引用。</summary>
    private void ResolveReferences()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (spawnArea == null && ServiceLocator.TryGet(out EnemySpawnerManager spawner))
        {
            spawnArea = spawner.GetComponent<SpawnAreaController>();
        }

        if (spawnArea == null)
        {
            spawnArea = FindFirstObjectByType<SpawnAreaController>();
        }

        if (backgroundRoot == null)
        {
            GameObject root = GameObject.Find("MapBackgroundRoot");
            backgroundRoot = root != null ? root.transform : transform;
        }
    }

    /// <summary>应用相机正交尺寸与背景色覆盖。</summary>
    /// <param name="bounds">相机边界配置。</param>
    private void ApplyCameraBounds(MapCameraBoundsConfig bounds)
    {
        if (mainCamera == null)
        {
            return;
        }

        if (bounds.OrthographicSizeOverride > 0f)
        {
            mainCamera.orthographicSize = bounds.OrthographicSizeOverride;
        }

        if (bounds.OverrideBackgroundColor)
        {
            mainCamera.backgroundColor = bounds.BackgroundColor;
        }
    }

    /// <summary>将生成区域配置下发至 <see cref="SpawnAreaController"/>。</summary>
    /// <param name="config">生成区域配置。</param>
    private void ApplySpawnArea(MapSpawnAreaConfig config)
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("[MapManager] 未找到 SpawnAreaController，跳过生成区域配置。");
            return;
        }

        spawnArea.ApplyMapConfig(config);
    }

    /// <summary>使用默认生成区域配置作为回退方案。</summary>
    private void ApplyFallbackSpawnArea()
    {
        if (spawnArea == null)
        {
            return;
        }

        spawnArea.ApplyMapConfig(MapSpawnAreaConfig.Default);
    }

    /// <summary>按配置调整墙体世界坐标（未启用场景默认时）。</summary>
    /// <param name="placement">墙体位置配置。</param>
    private void ApplyWallPlacement(MapWallPlacementConfig placement)
    {
        if (placement.UseSceneDefault || !WallControlManager.HasInstance)
        {
            return;
        }

        WallControlManager.Instance.transform.position = placement.WorldPosition;
    }

    /// <summary>按 Prefab、Sprite 或 Addressables 加载并实例化背景。</summary>
    /// <param name="mapData">地图配置。</param>
    private void ApplyBackground(MapDataSO mapData)
    {
        ClearBackgroundInstance();

        if (mapData.BackgroundPrefab != null)
        {
            activeBackgroundInstance = Instantiate(mapData.BackgroundPrefab, backgroundRoot);
            return;
        }

        if (mapData.BackgroundSprite != null)
        {
            activeBackgroundInstance = CreateSpriteBackground(mapData.BackgroundSprite);
            return;
        }

        if (!string.IsNullOrWhiteSpace(mapData.AddressableBackgroundKey) &&
            assetLoader.TryLoadSync(mapData.AddressableBackgroundKey, out Sprite remoteSprite) &&
            remoteSprite != null)
        {
            activeBackgroundInstance = CreateSpriteBackground(remoteSprite);
        }
    }

    /// <summary>用 Sprite 创建背景 GameObject。</summary>
    /// <param name="sprite">背景精灵。</param>
    /// <returns>创建的背景对象。</returns>
    private GameObject CreateSpriteBackground(Sprite sprite)
    {
        var go = new GameObject("MapBackground");
        go.transform.SetParent(backgroundRoot, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;
        return go;
    }

    /// <summary>销毁当前背景实例并清空引用。</summary>
    private void ClearBackgroundInstance()
    {
        if (activeBackgroundInstance == null)
        {
            return;
        }

        Destroy(activeBackgroundInstance);
        activeBackgroundInstance = null;
    }
}
