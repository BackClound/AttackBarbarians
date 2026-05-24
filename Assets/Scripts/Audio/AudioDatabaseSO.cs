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

    public IReadOnlyList<AudioConfigSO> Entries => entries;

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
