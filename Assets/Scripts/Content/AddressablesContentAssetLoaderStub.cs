using UnityEngine;

/// <summary>
/// Addressables 加载占位实现：当前项目未强制引入 Addressables，调用时会输出可定位警告。
/// </summary>
public sealed class AddressablesContentAssetLoaderStub : IContentAssetLoader
{
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
