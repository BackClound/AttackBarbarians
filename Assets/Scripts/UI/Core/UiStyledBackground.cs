using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 将 Image 背景设为科技废土钢板色（可在 Prefab 上挂多个）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UiStyledBackground : MonoBehaviour
{
    [SerializeField] private bool useRustPanel;
    [SerializeField] private float panelAlpha = 0.92f;

    /// <summary>按配置将 Image 背景设为钢板或锈蚀面板色。</summary>
    private void Awake()
    {
        var image = GetComponent<Image>();
        Color baseColor = useRustPanel
            ? UiTechWastelandPalette.PanelRust
            : UiTechWastelandPalette.PanelSteel;
        baseColor.a = panelAlpha;
        image.color = baseColor;
    }
}
