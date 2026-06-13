using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频总线：订阅 <see cref="GameEvents"/> 播放 BGM/SFX，应用 <see cref="SettingsData"/> 音量，SFX 走对象池。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 SaveManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 下子物体 <c>AudioManager</c>。</para>
/// <para><b>Inspector：</b>拖入 <see cref="AudioDatabaseSO"/>（留空则从 Resources 加载）。</para>
/// <para><b>测试：</b>运行后 UI 按钮应触发 SFX 事件；Boss/流程音乐由现有 Raise 调用驱动。</para>
/// </remarks>
public class AudioManager : MonoBehaviour, IGameSystem
{
    [Header("Config")]
    [SerializeField] private AudioDatabaseSO audioDatabase;

    [Header("Pool")]
    [SerializeField] private int sfxPoolInitialSize = 8;
    [SerializeField] private int sfxPoolMaxSize = 24;
    [SerializeField] private int maxConcurrentSfx = 12;

    [Header("Combat SFX")]
    [SerializeField] private string playerHurtSfxId = GameConstants.AudioIds.SfxPlayerHurt;
    [SerializeField] private string enemyHitSfxId = GameConstants.AudioIds.SfxEnemyHit;
    [SerializeField] private string enemyKillSfxId = GameConstants.AudioIds.SfxEnemyKill;
    [SerializeField] private string skillCastSfxId = GameConstants.AudioIds.SfxSkillCast;
    [SerializeField] private float combatHitMinInterval = 0.06f;

    private readonly Dictionary<string, AudioConfigSO> configById = new Dictionary<string, AudioConfigSO>(48);
    private readonly Dictionary<string, float> lastPlayTimeById = new Dictionary<string, float>(32);
    private readonly HashSet<string> missingClipLogged = new HashSet<string>();

    private Transform audioRoot;
    private AudioSource bgmSource;
    private AudioSource bossBgmSource;
    private AudioSourcePool sfxPool;
    private SaveManager saveManager;
    private string currentBgmId;
    private string currentBossBgmId;
    private bool enableLogs;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 加载音频数据库、构建索引、预加载剪辑并订阅游戏事件。
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        if (ServiceLocator.TryGet(out ConfigManager config) && config.GameConfig != null)
        {
            enableLogs = config.GameConfig.EnableRuntimeLogs;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance))
        {
            maxConcurrentSfx = performance.GetMaxConcurrentSfx();
        }

        EnsureHierarchy();
        LoadDatabase();
        BuildConfigIndex();
        PreloadMarkedClips();
        ApplyVolumeFromSave();
        SubscribeEvents();
        isInitialized = true;
    }

    /// <summary>
    /// 每帧回收已播放完毕的音效 AudioSource。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        sfxPool?.ReleaseFinished();
    }

    /// <summary>
    /// 取消事件订阅、停止所有音乐并清理缓存。
    /// </summary>
    public void Shutdown()
    {
        if (!isInitialized)
        {
            return;
        }

        UnsubscribeEvents();
        StopAllMusic();
        sfxPool?.StopAll();
        configById.Clear();
        lastPlayTimeById.Clear();
        missingClipLogged.Clear();
        isInitialized = false;
    }

    /// <summary>按 ID 播放音效（供事件与外部直接调用）。</summary>
    /// <param name="audioId">音频配置 ID。</param>
    public void PlaySfx(string audioId)
    {
        if (!TryGetConfig(audioId, out AudioConfigSO config))
        {
            return;
        }

        PlaySfxInternal(config, Vector3.zero, use3D: false);
    }

    /// <summary>按 ID 播放背景音乐（循环）。</summary>
    /// <param name="audioId">音频配置 ID。</param>
    public void PlayMusic(string audioId)
    {
        if (!TryGetConfig(audioId, out AudioConfigSO config))
        {
            return;
        }

        PlayMusicInternal(config);
    }

    /// <summary>
    /// 设置主音量并持久化到存档。
    /// </summary>
    /// <param name="volume">主音量（0 ~ 1）。</param>
    public void SetMasterVolume(float volume)
    {
        if (!TryGetSettings(out SettingsData settings))
        {
            return;
        }

        settings.masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeFromSave();
        MarkSettingsDirty();
    }

    /// <summary>
    /// 设置音乐音量并持久化到存档。
    /// </summary>
    /// <param name="volume">音乐音量（0 ~ 1）。</param>
    public void SetMusicVolume(float volume)
    {
        if (!TryGetSettings(out SettingsData settings))
        {
            return;
        }

        settings.musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeFromSave();
        MarkSettingsDirty();
    }

    /// <summary>
    /// 设置音效音量并持久化到存档。
    /// </summary>
    /// <param name="volume">音效音量（0 ~ 1）。</param>
    public void SetSfxVolume(float volume)
    {
        if (!TryGetSettings(out SettingsData settings))
        {
            return;
        }

        settings.sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeFromSave();
        MarkSettingsDirty();
    }

    /// <summary>
    /// 设置 UI 音量并持久化到存档。
    /// </summary>
    /// <param name="volume">UI 音量（0 ~ 1）。</param>
    public void SetUiVolume(float volume)
    {
        if (!TryGetSettings(out SettingsData settings))
        {
            return;
        }

        settings.uiVolume = Mathf.Clamp01(volume);
        ApplyVolumeFromSave();
        MarkSettingsDirty();
    }

    /// <summary>
    /// 调试菜单：播放 UI 点击音效。
    /// </summary>
    [ContextMenu("Debug/Play UI Click")]
    private void DebugPlayUiClick() => PlaySfx(GameConstants.AudioIds.SfxUiClick);

    /// <summary>
    /// 调试菜单：播放主菜单背景音乐。
    /// </summary>
    [ContextMenu("Debug/Play Main Menu BGM")]
    private void DebugPlayMainMenuBgm() => PlayMusic(GameConstants.AudioIds.MusicMainMenu);

    /// <summary>
    /// 确保音频层级结构（根节点、BGM 源、音效池）已创建。
    /// </summary>
    private void EnsureHierarchy()
    {
        if (audioRoot == null)
        {
            var rootGo = new GameObject("AudioRoot");
            rootGo.transform.SetParent(transform, false);
            audioRoot = rootGo.transform;
        }

        bgmSource = EnsureMusicSource("BGM", bgmSource);
        bossBgmSource = EnsureMusicSource("BossBGM", bossBgmSource);
        sfxPool = new AudioSourcePool(audioRoot, "SfxPool", sfxPoolInitialSize, sfxPoolMaxSize);
    }

    /// <summary>
    /// 确保指定名称的音乐 AudioSource 已创建。
    /// </summary>
    /// <param name="name">AudioSource 节点名称。</param>
    /// <param name="existing">已有的 AudioSource，为 null 时新建。</param>
    /// <returns>可用的音乐 AudioSource。</returns>
    private AudioSource EnsureMusicSource(string name, AudioSource existing)
    {
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject(name);
        go.transform.SetParent(audioRoot, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        return source;
    }

    /// <summary>
    /// 从 Inspector 或 Resources 加载音频数据库。
    /// </summary>
    private void LoadDatabase()
    {
        if (audioDatabase == null)
        {
            audioDatabase = Resources.Load<AudioDatabaseSO>(GameConstants.ResourcePaths.AudioDatabase);
        }

        if (audioDatabase == null && enableLogs)
        {
            Debug.LogWarning("[AudioManager] AudioDatabase 未配置，音频将静音。");
        }
    }

    /// <summary>
    /// 将数据库条目构建为 ID → 配置 的查找索引。
    /// </summary>
    private void BuildConfigIndex()
    {
        configById.Clear();
        if (audioDatabase?.Entries == null)
        {
            return;
        }

        IReadOnlyList<AudioConfigSO> entries = audioDatabase.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            AudioConfigSO entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.AudioId))
            {
                continue;
            }

            configById[entry.AudioId] = entry;
        }
    }

    /// <summary>
    /// 预加载标记了 Preload 的音频剪辑数据。
    /// </summary>
    private void PreloadMarkedClips()
    {
        if (audioDatabase?.Entries == null)
        {
            return;
        }

        IReadOnlyList<AudioConfigSO> entries = audioDatabase.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            AudioConfigSO entry = entries[i];
            if (entry == null || !entry.Preload || entry.Clip == null)
            {
                continue;
            }

            entry.Clip.LoadAudioData();
        }
    }

    /// <summary>
    /// 订阅音频与战斗相关的游戏事件。
    /// </summary>
    private void SubscribeEvents()
    {
        GameEvents.SubscribeAudioPlaySfx(OnAudioPlaySfx);
        GameEvents.SubscribeAudioPlayMusic(OnAudioPlayMusic);
        GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.SubscribePlayerDamaged(OnPlayerDamaged);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribePlayerSkillCast(OnPlayerSkillCast);
        GameEvents.SubscribeProjectileHit(OnProjectileHit);
        GameEvents.SubscribeBossDefeated(OnBossDefeated);
    }

    /// <summary>
    /// 取消所有已订阅的游戏事件。
    /// </summary>
    private void UnsubscribeEvents()
    {
        GameEvents.UnsubscribeAudioPlaySfx(OnAudioPlaySfx);
        GameEvents.UnsubscribeAudioPlayMusic(OnAudioPlayMusic);
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.UnsubscribePlayerDamaged(OnPlayerDamaged);
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribePlayerSkillCast(OnPlayerSkillCast);
        GameEvents.UnsubscribeProjectileHit(OnProjectileHit);
        GameEvents.UnsubscribeBossDefeated(OnBossDefeated);
    }

    /// <summary>
    /// 响应音频播放音效事件。
    /// </summary>
    /// <param name="ctx">游戏事件上下文，Payload 为音频 ID 字符串。</param>
    private void OnAudioPlaySfx(GameEventContext ctx)
    {
        if (ctx.Payload is string audioId)
        {
            PlaySfx(audioId);
        }
    }

    /// <summary>
    /// 响应音频播放音乐事件。
    /// </summary>
    /// <param name="ctx">游戏事件上下文，Payload 为音频 ID 字符串。</param>
    private void OnAudioPlayMusic(GameEventContext ctx)
    {
        if (ctx.Payload is string audioId)
        {
            PlayMusic(audioId);
        }
    }

    /// <summary>
    /// 存档加载后重新应用音量设置。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext ctx) => ApplyVolumeFromSave();

    /// <summary>
    /// 玩家受伤时播放受伤音效。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnPlayerDamaged(GameEventContext ctx)
    {
        if (string.IsNullOrEmpty(playerHurtSfxId))
        {
            return;
        }

        PlaySfx(playerHurtSfxId);
    }

    /// <summary>
    /// 敌人被击杀时播放击杀音效。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (string.IsNullOrEmpty(enemyKillSfxId))
        {
            return;
        }

        PlaySfx(enemyKillSfxId);
    }

    /// <summary>
    /// 玩家施放技能时播放施法音效。
    /// </summary>
    /// <param name="ctx">游戏事件上下文，Payload 为技能 ID。</param>
    private void OnPlayerSkillCast(GameEventContext ctx)
    {
        if (ctx.Payload is not string skillId || string.IsNullOrEmpty(skillCastSfxId))
        {
            return;
        }

        PlaySfx(skillCastSfxId);
    }

    /// <summary>
    /// Boss 被击败时停止 Boss 背景音乐。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnBossDefeated(GameEventContext ctx) => StopBossMusic();

    /// <summary>
    /// 投射物命中敌人时播放命中音效（带节流）。
    /// </summary>
    /// <param name="ctx">游戏事件上下文，Payload 为 <see cref="ProjectileHitEventArgs"/>。</param>
    private void OnProjectileHit(GameEventContext ctx)
    {
        if (ctx.Payload is not ProjectileHitEventArgs args || args.Target == null)
        {
            return;
        }

        if (!args.Target.CompareTag(GameConstants.Tags.Enemy))
        {
            return;
        }

        if (string.IsNullOrEmpty(enemyHitSfxId))
        {
            return;
        }

        if (!TryGetConfig(enemyHitSfxId, out AudioConfigSO config))
        {
            return;
        }

        float interval = config.MinIntervalSeconds > 0f ? config.MinIntervalSeconds : combatHitMinInterval;
        if (!CanPlayThrottled(enemyHitSfxId, interval))
        {
            return;
        }

        PlaySfxInternal(config, args.Target.transform.position, use3D: false);
    }

    /// <summary>
    /// 内部播放背景音乐，处理 BGM/Boss 通道切换与音量叠加。
    /// </summary>
    /// <param name="config">音频配置。</param>
    private void PlayMusicInternal(AudioConfigSO config)
    {
        if (!TryValidateClip(config, out AudioClip clip))
        {
            return;
        }

        bool isBossChannel = config.Channel == AudioChannel.Boss;
        AudioSource targetSource = isBossChannel ? bossBgmSource : bgmSource;
        string activeId = isBossChannel ? currentBossBgmId : currentBgmId;

        if (activeId == config.AudioId && targetSource.isPlaying)
        {
            return;
        }

        if (isBossChannel)
        {
            currentBossBgmId = config.AudioId;
        }
        else
        {
            StopBossMusic();
            currentBgmId = config.AudioId;
        }
        ConfigureSource(targetSource, config, clip, config.Loop);
        targetSource.volume = ResolveChannelVolume(config.Channel) * config.Volume;
        targetSource.Play();

        if (config.Channel == AudioChannel.Boss && bgmSource.isPlaying)
        {
            bgmSource.volume = ResolveChannelVolume(AudioChannel.Bgm) * 0.35f;
        }
        else if (config.Channel != AudioChannel.Boss && bossBgmSource.isPlaying)
        {
            bossBgmSource.volume = ResolveChannelVolume(AudioChannel.Boss) * config.Volume;
        }
    }

    /// <summary>
    /// 内部播放音效，经对象池租借 AudioSource 并应用节流与并发限制。
    /// </summary>
    /// <param name="config">音频配置。</param>
    /// <param name="worldPosition">3D 音效的世界坐标位置。</param>
    /// <param name="use3D">是否以 3D 空间音效播放。</param>
    private void PlaySfxInternal(AudioConfigSO config, Vector3 worldPosition, bool use3D)
    {
        if (!TryValidateClip(config, out AudioClip clip))
        {
            return;
        }

        if (!IsSfxChannel(config.Channel))
        {
            if (enableLogs)
            {
                Debug.LogWarning($"[AudioManager] {config.AudioId} 通道为 {config.Channel}，请使用 PlayMusic。");
            }

            return;
        }

        float interval = config.MinIntervalSeconds > 0f ? config.MinIntervalSeconds : 0f;
        if (interval > 0f && !CanPlayThrottled(config.AudioId, interval))
        {
            return;
        }

        if (sfxPool.ActiveCount >= maxConcurrentSfx)
        {
            return;
        }

        if (!sfxPool.TryRent(out AudioSource source))
        {
            return;
        }

        lastPlayTimeById[config.AudioId] = Time.unscaledTime;
        ConfigureSource(source, config, clip, loop: false);
        source.volume = ResolveChannelVolume(config.Channel) * config.Volume;
        source.spatialBlend = use3D ? 1f : 0f;
        if (use3D)
        {
            source.transform.position = worldPosition;
        }

        source.Play();
    }

    /// <summary>
    /// 将配置参数应用到 AudioSource。
    /// </summary>
    /// <param name="source">目标 AudioSource。</param>
    /// <param name="config">音频配置。</param>
    /// <param name="clip">要播放的音频剪辑。</param>
    /// <param name="loop">是否循环播放。</param>
    private static void ConfigureSource(AudioSource source, AudioConfigSO config, AudioClip clip, bool loop)
    {
        source.clip = clip;
        source.loop = loop;
        source.pitch = config.Pitch;
        source.outputAudioMixerGroup = config.MixerGroup;
    }

    /// <summary>
    /// 按音频 ID 查找配置，未找到时记录一次警告。
    /// </summary>
    /// <param name="audioId">音频配置 ID。</param>
    /// <param name="config">找到时输出的配置实例。</param>
    /// <returns>找到返回 true，否则返回 false。</returns>
    private bool TryGetConfig(string audioId, out AudioConfigSO config)
    {
        config = null;
        if (string.IsNullOrWhiteSpace(audioId))
        {
            return false;
        }

        if (configById.TryGetValue(audioId, out config))
        {
            return true;
        }

        if (missingClipLogged.Add(audioId) && enableLogs)
        {
            Debug.LogWarning($"[AudioManager] 未找到音频配置: {audioId}");
        }

        return false;
    }

    /// <summary>
    /// 校验配置是否包含有效的 AudioClip。
    /// </summary>
    /// <param name="config">音频配置。</param>
    /// <param name="clip">校验通过时输出的音频剪辑。</param>
    /// <returns>有效返回 true，否则返回 false。</returns>
    private bool TryValidateClip(AudioConfigSO config, out AudioClip clip)
    {
        clip = config.Clip;
        if (clip != null)
        {
            return true;
        }

        if (missingClipLogged.Add(config.AudioId) && enableLogs)
        {
            Debug.LogWarning($"[AudioManager] {config.AudioId} 无 AudioClip，已跳过。");
        }

        return false;
    }

    /// <summary>
    /// 检查指定音频 ID 是否已过最短播放间隔。
    /// </summary>
    /// <param name="audioId">音频配置 ID。</param>
    /// <param name="intervalSeconds">最短间隔（秒）。</param>
    /// <returns>允许播放返回 true，仍在冷却中返回 false。</returns>
    private bool CanPlayThrottled(string audioId, float intervalSeconds)
    {
        if (intervalSeconds <= 0f)
        {
            return true;
        }

        if (lastPlayTimeById.TryGetValue(audioId, out float lastTime) &&
            Time.unscaledTime - lastTime < intervalSeconds)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 判断通道是否属于短音效类（Sfx 或 Ui）。
    /// </summary>
    /// <param name="channel">音频通道。</param>
    /// <returns>是音效通道返回 true，否则返回 false。</returns>
    private static bool IsSfxChannel(AudioChannel channel) =>
        channel == AudioChannel.Sfx || channel == AudioChannel.Ui;

    /// <summary>
    /// 根据通道类型与存档设置计算最终音量系数。
    /// </summary>
    /// <param name="channel">音频通道。</param>
    /// <returns>主音量 × 通道音量的组合系数。</returns>
    private float ResolveChannelVolume(AudioChannel channel)
    {
        if (!TryGetSettings(out SettingsData settings))
        {
            return 1f;
        }

        float master = Mathf.Clamp01(settings.masterVolume);
        return channel switch
        {
            AudioChannel.Bgm => master * Mathf.Clamp01(settings.musicVolume),
            AudioChannel.Boss => master * Mathf.Clamp01(settings.musicVolume),
            AudioChannel.Ambient => master * Mathf.Clamp01(settings.musicVolume),
            AudioChannel.Ui => master * Mathf.Clamp01(settings.uiVolume > 0f ? settings.uiVolume : settings.sfxVolume),
            _ => master * Mathf.Clamp01(settings.sfxVolume),
        };
    }

    /// <summary>
    /// 根据存档设置刷新当前正在播放的 BGM 与 Boss BGM 音量。
    /// </summary>
    private void ApplyVolumeFromSave()
    {
        if (bgmSource != null && bgmSource.isPlaying && TryGetConfig(currentBgmId, out AudioConfigSO bgmConfig))
        {
            bgmSource.volume = ResolveChannelVolume(bgmConfig.Channel) * bgmConfig.Volume;
        }

        if (bossBgmSource != null && bossBgmSource.isPlaying &&
            TryGetConfig(currentBossBgmId, out AudioConfigSO bossConfig))
        {
            bossBgmSource.volume = ResolveChannelVolume(bossConfig.Channel) * bossConfig.Volume;
        }
    }

    /// <summary>
    /// 停止 Boss 背景音乐并恢复普通 BGM 音量。
    /// </summary>
    private void StopBossMusic()
    {
        if (bossBgmSource != null && bossBgmSource.isPlaying)
        {
            bossBgmSource.Stop();
        }

        currentBossBgmId = null;

        if (bgmSource != null && bgmSource.isPlaying &&
            TryGetConfig(currentBgmId, out AudioConfigSO bgmConfig))
        {
            bgmSource.volume = ResolveChannelVolume(bgmConfig.Channel) * bgmConfig.Volume;
        }
    }

    /// <summary>
    /// 停止所有背景音乐并清除当前播放 ID。
    /// </summary>
    private void StopAllMusic()
    {
        bgmSource?.Stop();
        bossBgmSource?.Stop();
        currentBgmId = null;
        currentBossBgmId = null;
    }

    /// <summary>
    /// 尝试从 SaveManager 获取设置数据。
    /// </summary>
    /// <param name="settings">获取成功时输出的设置数据。</param>
    /// <returns>获取成功返回 true，否则返回 false。</returns>
    private bool TryGetSettings(out SettingsData settings)
    {
        settings = saveManager != null ? saveManager.Settings : null;
        return settings != null;
    }

    /// <summary>
    /// 标记存档为脏，触发持久化。
    /// </summary>
    private void MarkSettingsDirty()
    {
        if (saveManager == null)
        {
            ServiceLocator.TryGet(out saveManager);
        }

        saveManager?.MarkDirty();
    }
}
