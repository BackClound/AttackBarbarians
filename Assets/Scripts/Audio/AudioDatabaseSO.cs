using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频配置总表，供 <see cref="AudioManager"/> 按 ID 索引。
/// </summary>
/// <remarks>
/// <para><b>路径：</b><c>Assets/Resources/Config/Audio/AudioDatabase.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "AudioDatabase", menuName = "Attack Barbarians/Audio/Audio Database")]
public class AudioDatabaseSO : ScriptableObject
{
    [SerializeField] private List<AudioConfigSO> entries = new List<AudioConfigSO>(32);

    /// <summary>所有音频配置条目的只读列表。</summary>
    public IReadOnlyList<AudioConfigSO> Entries => entries;

    /// <summary>
    /// 按音频 ID 查找配置。
    /// </summary>
    /// <param name="audioId">音频配置 ID。</param>
    /// <param name="config">找到时输出的配置实例。</param>
    /// <returns>找到返回 true，否则返回 false。</returns>
    public bool TryGet(string audioId, out AudioConfigSO config)
    {
        config = null;
        if (string.IsNullOrWhiteSpace(audioId) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            AudioConfigSO entry = entries[i];
            if (entry != null && entry.AudioId == audioId)
            {
                config = entry;
                return true;
            }
        }

        return false;
    }
}
