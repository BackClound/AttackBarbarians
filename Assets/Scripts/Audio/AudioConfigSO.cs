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

    public AudioChannel Channel => channel;
    public AudioClip Clip => clip;
    public float Volume => volume;
    public float Pitch => pitch;
    public bool Loop => loop;
    public bool Preload => preload;
    public AudioMixerGroup MixerGroup => mixerGroup;
    public float MinIntervalSeconds => minIntervalSeconds;
    public int Priority => priority;

    public string AudioId => ConfigId;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (clip == null)
        {
            result.AddWarning(name, "未分配 AudioClip，运行时将静音。");
        }
    }
}
