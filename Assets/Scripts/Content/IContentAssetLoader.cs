using UnityEngine;

/// <summary>
/// 内容资产加载接口，预留 Addressables 热更新路径。
/// </summary>
public interface IContentAssetLoader
{
    bool TryLoadSync<T>(string key, out T asset) where T : Object;
}
