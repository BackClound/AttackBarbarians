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

    public bool IsInitialized => isInitialized;
    public bool IsMapLoaded => currentMap != null;
    public MapDataSO CurrentMap => currentMap;

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        ClearBackgroundInstance();
        MapRuntimeContext.Reset();
        currentMap = null;
        isInitialized = false;
    }

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

    private void ApplySpawnArea(MapSpawnAreaConfig config)
    {
        if (spawnArea == null)
        {
            Debug.LogWarning("[MapManager] 未找到 SpawnAreaController，跳过生成区域配置。");
            return;
        }

        spawnArea.ApplyMapConfig(config);
    }

    private void ApplyFallbackSpawnArea()
    {
        if (spawnArea == null)
        {
            return;
        }

        spawnArea.ApplyMapConfig(MapSpawnAreaConfig.Default);
    }

    private void ApplyWallPlacement(MapWallPlacementConfig placement)
    {
        if (placement.UseSceneDefault || !WallControlManager.HasInstance)
        {
            return;
        }

        WallControlManager.Instance.transform.position = placement.WorldPosition;
    }

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

    private GameObject CreateSpriteBackground(Sprite sprite)
    {
        var go = new GameObject("MapBackground");
        go.transform.SetParent(backgroundRoot, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = -100;
        return go;
    }

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
