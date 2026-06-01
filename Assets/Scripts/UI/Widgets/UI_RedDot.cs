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

    private void Awake()
    {
        if (dotRoot == null)
        {
            dotRoot = gameObject;
        }
    }

    public void SetVisible(bool visible)
    {
        if (dotRoot != null)
        {
            dotRoot.SetActive(visible);
        }
    }
}
