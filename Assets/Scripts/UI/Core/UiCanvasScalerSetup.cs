using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 强制竖屏 Canvas Scaler：1080×1920，Scale With Screen Size。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 UI 根 Canvas 上。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasScaler))]
public class UiCanvasScalerSetup : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    private void Awake()
    {
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }
}
