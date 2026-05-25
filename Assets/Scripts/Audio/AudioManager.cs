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

    public bool IsInitialized => isInitialized;

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

    public void Tick(float deltaTime)
    {
        if (!isInitialized)
        {
            return;
        }

        sfxPool?.ReleaseFinished();
    }

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
    public void PlaySfx(string audioId)
    {
        if (!TryGetConfig(audioId, out AudioConfigSO config))
        {
            return;
        }

        PlaySfxInternal(config, Vector3.zero, use3D: false);
    }

    /// <summary>按 ID 播放背景音乐（循环）。</summary>
    public void PlayMusic(string audioId)
    {
        if (!TryGetConfig(audioId, out AudioConfigSO config))
        {
            return;
        }

        PlayMusicInternal(config);
    }

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

    [ContextMenu("Debug/Play UI Click")]
    private void DebugPlayUiClick() => PlaySfx(GameConstants.AudioIds.SfxUiClick);

    [ContextMenu("Debug/Play Main Menu BGM")]
    private void DebugPlayMainMenuBgm() => PlayMusic(GameConstants.AudioIds.MusicMainMenu);

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

    private void OnAudioPlaySfx(GameEventContext ctx)
    {
        if (ctx.Payload is string audioId)
        {
            PlaySfx(audioId);
        }
    }

    private void OnAudioPlayMusic(GameEventContext ctx)
    {
        if (ctx.Payload is string audioId)
        {
            PlayMusic(audioId);
        }
    }

    private void OnSaveLoaded(GameEventContext ctx) => ApplyVolumeFromSave();

    private void OnPlayerDamaged(GameEventContext ctx)
    {
        if (string.IsNullOrEmpty(playerHurtSfxId))
        {
            return;
        }

        PlaySfx(playerHurtSfxId);
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (string.IsNullOrEmpty(enemyKillSfxId))
        {
            return;
        }

        PlaySfx(enemyKillSfxId);
    }

    private void OnPlayerSkillCast(GameEventContext ctx)
    {
        if (ctx.Payload is not string skillId || string.IsNullOrEmpty(skillCastSfxId))
        {
            return;
        }

        PlaySfx(skillCastSfxId);
    }

    private void OnBossDefeated(GameEventContext ctx) => StopBossMusic();

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

    private static void ConfigureSource(AudioSource source, AudioConfigSO config, AudioClip clip, bool loop)
    {
        source.clip = clip;
        source.loop = loop;
        source.pitch = config.Pitch;
        source.outputAudioMixerGroup = config.MixerGroup;
    }

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

    private static bool IsSfxChannel(AudioChannel channel) =>
        channel == AudioChannel.Sfx || channel == AudioChannel.Ui;

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

    private void StopAllMusic()
    {
        bgmSource?.Stop();
        bossBgmSource?.Stop();
        currentBgmId = null;
        currentBossBgmId = null;
    }

    private bool TryGetSettings(out SettingsData settings)
    {
        settings = saveManager != null ? saveManager.Settings : null;
        return settings != null;
    }

    private void MarkSettingsDirty()
    {
        if (saveManager == null)
        {
            ServiceLocator.TryGet(out saveManager);
        }

        saveManager?.MarkDirty();
    }
}
