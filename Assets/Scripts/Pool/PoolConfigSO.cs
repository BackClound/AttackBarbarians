using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池配置资产：集中管理各池的 Prefab、预热与容量。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject）。</para>
/// <para><b>创建方式：</b>Project 右键 → Create → Attack Barbarians → Config → Pool Config。</para>
/// <para><b>引用方式：</b>拖到 <see cref="PoolManager"/> 的 Pool Config 槽位；可与 Inspector 内 Pools 列表合并（同 Key 时 Inspector 优先）。</para>
/// </remarks>
[CreateAssetMenu(fileName = "PoolConfig", menuName = "Attack Barbarians/Config/Pool Config")]
public class PoolConfigSO : ScriptableObject
{
    [SerializeField] private List<PoolEntry> entries = new List<PoolEntry>();

    public IReadOnlyList<PoolEntry> Entries => entries;
}
