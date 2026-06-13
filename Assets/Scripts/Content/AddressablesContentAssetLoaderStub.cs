using UnityEngine;

/// <summary>
/// Addressables 加载占位实现：当前项目未强制引入 Addressables，调用时会输出可定位警告。
/// </summary>
public sealed class AddressablesContentAssetLoaderStub : IContentAssetLoader
{
    /// <summary>
    /// 尝试通过 Addressables 同步加载（当前为占位实现，始终失败）。
    /// </summary>
    /// <typeparam name="T">要加载的 Unity 资产类型。</typeparam>
    /// <param name="key">Addressables 地址。</param>
    /// <param name="asset">加载成功时输出的资产实例。</param>
    /// <returns>当前始终返回 false 并输出警告。</returns>
    public bool TryLoadSync<T>(string key, out T asset) where T : Object
    {
        asset = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        Debug.LogWarning(
            $"[AddressablesContentAssetLoaderStub] Addressables 尚未接入，无法加载 key={key}。" +
            "请改用 ResourcesContentAssetLoader 或在后续阶段替换为正式 Addressables 实现。");
        return false;
    }
}
