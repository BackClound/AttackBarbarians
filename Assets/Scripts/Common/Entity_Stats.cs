using UnityEngine;

/// <summary>
///包含Player和Enemy的基础攻击数据和共有数据
/// </summary>
public class Entity_Stats : MonoBehaviour
{
    [Header("基本数据")]
    public MajorGroupStats majorStats;

    [Header("攻击属性")]
    public OffenseGroupStats offenseStats;

    [Header("抗性")]
    public DefenseGroupStats defenseStats;


}
