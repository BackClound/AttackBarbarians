using UnityEngine;

/// <summary>
/// 基于 Resources 的同步内容加载器（默认实现）。
/// </summary>
public sealed class ResourcesContentAssetLoader : IContentAssetLoader
{
    /// <summary>
    /// 同步加载指定 key 的 Resources 资产。
    /// </summary>
    /// <typeparam name="T">要加载的 Unity 资产类型。</typeparam>
    /// <param name="key">Resources 路径。</param>
    /// <param name="asset">加载成功时输出的资产实例。</param>
    /// <returns>加载成功返回 true，否则返回 false。</returns>
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
