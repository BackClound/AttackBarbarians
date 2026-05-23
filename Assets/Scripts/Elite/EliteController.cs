using UnityEngine;

/// <summary>
/// 精英个体标记：生成时应用额外倍率并发布 <see cref="GameEvents.RaiseEliteSpawned"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>可选。缺失时 <see cref="EnemySpawnerManager"/> 会在标记为精英时自动添加。</para>
/// </remarks>
[DisallowMultipleComponent]
public class EliteController : MonoBehaviour
{
    private bool isEliteSpawn;
    private string enemyConfigId = string.Empty;

    public bool IsEliteSpawn => isEliteSpawn;

    public void Initialize(string configId, bool eliteModeActive)
    {
        isEliteSpawn = true;
        enemyConfigId = configId ?? string.Empty;

        GameEvents.RaiseEliteSpawned(this, new EliteSpawnedEventArgs(gameObject, enemyConfigId, eliteModeActive));
    }

    public void ResetForPool()
    {
        isEliteSpawn = false;
        enemyConfigId = string.Empty;
    }
}
