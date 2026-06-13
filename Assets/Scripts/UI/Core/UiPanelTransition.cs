using System.Collections;
using UnityEngine;

/// <summary>
/// 面板进出场：自下而上位移 + Alpha（不依赖 DoTween）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。与 <see cref="UiPanelBase"/> 同物体。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class UiPanelTransition : MonoBehaviour
{
    [SerializeField] private float slidePixels = 16f;
    [SerializeField] private float durationSeconds = 0.18f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 shownAnchoredPosition;
    private Coroutine routine;

    /// <summary>缓存 CanvasGroup 与 RectTransform，记录显示位置。</summary>
    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            shownAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    /// <summary>播放面板进场动画（上移 + 淡入）。</summary>
    public void PlayShow()
    {
        StopRoutine();
        routine = StartCoroutine(AnimateShow());
    }

    /// <summary>播放面板退场动画，完成后执行回调。</summary>
        /// <param name="onComplete">动画结束后的回调。</param>
    public void PlayHide(System.Action onComplete)
    {
        StopRoutine();
        routine = StartCoroutine(AnimateHide(onComplete));
    }

    /// <summary>立即将面板设为隐藏态（透明、下移、不可交互）。</summary>
    public void SnapHidden()
    {
        StopRoutine();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = shownAnchoredPosition + Vector2.down * slidePixels;
        }
    }

    /// <summary>立即将面板设为完全显示态。</summary>
    public void SnapShown()
    {
        StopRoutine();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = shownAnchoredPosition;
        }
    }

    /// <summary>协程：执行进场淡入与上移动画。</summary>
    private IEnumerator AnimateShow()
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        Vector2 from = shownAnchoredPosition + Vector2.down * slidePixels;
        Vector2 to = shownAnchoredPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / durationSeconds);
            canvasGroup.alpha = t;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
            }

            yield return null;
        }

        SnapShown();
        routine = null;
    }

    /// <summary>协程：执行退场淡出与下移动画。</summary>
    private IEnumerator AnimateHide(System.Action onComplete)
    {
        if (canvasGroup == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        Vector2 from = rectTransform != null ? rectTransform.anchoredPosition : shownAnchoredPosition;
        Vector2 to = shownAnchoredPosition + Vector2.down * slidePixels;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < durationSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / durationSeconds);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
            }

            yield return null;
        }

        SnapHidden();
        onComplete?.Invoke();
        routine = null;
    }

    /// <summary>停止当前过渡协程。</summary>
    private void StopRoutine()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}
