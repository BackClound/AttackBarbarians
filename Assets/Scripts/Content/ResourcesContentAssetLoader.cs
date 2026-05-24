using UnityEngine;

/// <summary>
/// 基于 Resources 的同步内容加载器（默认实现）。
/// </summary>
public sealed class ResourcesContentAssetLoader : IContentAssetLoader
{
    public bool TryLoadSync<T>(string key, out T asset) where T : Object
    {
        asset = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        asset = Resources.Load<T>(key);
        return asset != null;
    }
}
