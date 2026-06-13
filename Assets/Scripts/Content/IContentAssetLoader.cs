using UnityEngine;

/// <summary>
/// 内容资产加载接口，预留 Addressables 热更新路径。
/// </summary>
public interface IContentAssetLoader
{
    /// <summary>
    /// 同步加载指定 key 的内容资产。
    /// </summary>
    /// <typeparam name="T">要加载的 Unity 资产类型。</typeparam>
    /// <param name="key">资产路径或 Addressables 地址。</param>
    /// <param name="asset">加载成功时输出的资产实例。</param>
    /// <returns>加载成功返回 true，否则返回 false。</returns>
    bool TryLoadSync<T>(string key, out T asset) where T : Object;
}
