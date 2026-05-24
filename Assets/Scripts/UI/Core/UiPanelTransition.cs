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

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            shownAnchoredPosition = rectTransform.anchoredPosition;
        }
    }

    public void PlayShow()
    {
        StopRoutine();
        routine = StartCoroutine(AnimateShow());
    }

    public void PlayHide(System.Action onComplete)
    {
        StopRoutine();
        routine = StartCoroutine(AnimateHide(onComplete));
    }

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

    private void StopRoutine()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }
}
