using UnityEngine;

/// <summary>
/// 跨场景保留的 MonoBehaviour 单例基类（默认 <c>DontDestroyOnLoad</c>）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（抽象基类）。由 <see cref="GameBootstrapper"/> 等常驻系统继承。</para>
/// <para><b>推荐挂载：</b><c>GameSystems</c> 根物体。</para>
/// </remarks>
public abstract class PersistentMonoSingleton<T> : MonoSingleton<T> where T : PersistentMonoSingleton<T>
{
    protected override SingletonOptions Options => SingletonOptions.PersistentDefault;
}
