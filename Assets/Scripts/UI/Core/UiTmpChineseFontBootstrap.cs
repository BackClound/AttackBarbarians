using UnityEngine;

/// <summary>
/// 挂在 UI 根节点：进入场景后为子树内所有 TMP 应用中文字体。
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class UiTmpChineseFontBootstrap : MonoBehaviour
{
    /// <summary>场景加载时为 UI 子树批量应用中文字体。</summary>
    private void Awake()
    {
        UiTmpChineseFont.ApplyAllInChildren(transform);
    }
}
