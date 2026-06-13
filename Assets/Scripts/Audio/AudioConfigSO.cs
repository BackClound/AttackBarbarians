using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 单条音频配置：ID、Clip、通道、音量与播放策略。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Audio → Audio Config。</para>
/// <para><b>ID 规范：</b><c>audio.music.*</c> / <c>audio.sfx.*</c>，与 <see cref="GameConstants.AudioIds"/> 一致。</para>
/// </remarks>
[CreateAssetMenu(fileName = "AudioConfig", menuName = "Attack Barbarians/Audio/Audio Config")]
public class AudioConfigSO : ConfigDataBase
{
    [Header("Playback")]
    [SerializeField] private AudioChannel channel = AudioChannel.Sfx;
    [SerializeField] private AudioClip clip;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    [Range(-3f, 3f)]
    [SerializeField] private float pitch = 1f;
    [SerializeField] private bool loop;
    [SerializeField] private bool preload;
    [SerializeField] private AudioMixerGroup mixerGroup;

    [Header("Limits")]
    [Tooltip("同一 ID 最短播放间隔（秒），0 表示不限制。")]
    [SerializeField] private float minIntervalSeconds;
    [Tooltip("数值越大越不易被并发上限挤掉。")]
    [SerializeField] private int priority;

    /// <summary>音频播放通道。</summary>
    public AudioChannel Channel => channel;

    /// <summary>音频剪辑资源。</summary>
    public AudioClip Clip => clip;

    /// <summary>播放音量（0 ~ 1）。</summary>
    public float Volume => volume;

    /// <summary>播放音高偏移。</summary>
    public float Pitch => pitch;

    /// <summary>是否循环播放。</summary>
    public bool Loop => loop;

    /// <summary>是否在启动时预加载音频数据。</summary>
    public bool Preload => preload;

    /// <summary>输出混音组。</summary>
    public AudioMixerGroup MixerGroup => mixerGroup;

    /// <summary>同一 ID 最短播放间隔（秒），0 表示不限制。</summary>
    public float MinIntervalSeconds => minIntervalSeconds;

    /// <summary>播放优先级，数值越大越不易被并发上限挤掉。</summary>
    public int Priority => priority;

    /// <summary>音频配置 ID，等同于 <see cref="ConfigDataBase.ConfigId"/>。</summary>
    public string AudioId => ConfigId;

    /// <summary>
    /// 收集本配置的校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (clip == null)
        {
            result.AddWarning(name, "未分配 AudioClip，运行时将静音。");
        }
    }
}
