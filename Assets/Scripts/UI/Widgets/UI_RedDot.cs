using UnityEngine;

/// <summary>
/// 红点提示：控制通知圆点显隐。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在红点 Image 根物体上。</para>
/// </remarks>
public class UI_RedDot : MonoBehaviour
{
    [SerializeField] private GameObject dotRoot;

    /// <summary>若未绑定则默认使用自身 GameObject 作为红点根节点。</summary>
    private void Awake()
    {
        if (dotRoot == null)
        {
            dotRoot = gameObject;
        }
    }

    /// <summary>控制红点显隐。</summary>
    /// <param name="visible">为 <c>true</c> 时显示红点。</param>
    public void SetVisible(bool visible)
    {
        if (dotRoot != null)
        {
            dotRoot.SetActive(visible);
        }
    }
}
