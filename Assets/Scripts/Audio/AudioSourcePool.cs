using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// <see cref="AudioSource"/> 对象池，供短音效复用，避免高频 Instantiate。
/// </summary>
internal sealed class AudioSourcePool
{
    private readonly Transform root;
    private readonly Stack<AudioSource> idle = new Stack<AudioSource>(16);
    private readonly List<AudioSource> active = new List<AudioSource>(16);
    private readonly int maxSize;

    /// <summary>当前正在播放的 AudioSource 数量。</summary>
    public int ActiveCount => active.Count;

    /// <summary>
    /// 创建 AudioSource 对象池并预热初始实例。
    /// </summary>
    /// <param name="parent">池根节点的父 Transform。</param>
    /// <param name="poolName">池 GameObject 名称。</param>
    /// <param name="initialSize">初始预热数量。</param>
    /// <param name="maxSize">最大并发借出数量。</param>
    public AudioSourcePool(Transform parent, string poolName, int initialSize, int maxSize)
    {
        this.maxSize = Mathf.Max(initialSize, maxSize);
        root = new GameObject(poolName).transform;
        root.SetParent(parent, false);

        for (int i = 0; i < initialSize; i++)
        {
            idle.Push(CreateSource());
        }
    }

    /// <summary>
    /// 从池中借出一个 AudioSource 用于播放。
    /// </summary>
    /// <param name="source">借出的 AudioSource；池满时为 null。</param>
    /// <returns>借出成功返回 true，否则 false。</returns>
    public bool TryRent(out AudioSource source)
    {
        if (active.Count >= maxSize)
        {
            source = null;
            return false;
        }

        source = idle.Count > 0 ? idle.Pop() : CreateSource();
        source.gameObject.SetActive(true);
        active.Add(source);
        return true;
    }

    /// <summary>
    /// 回收已播放完毕的 AudioSource 到空闲栈。
    /// </summary>
    public void ReleaseFinished()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            AudioSource source = active[i];
            if (source == null)
            {
                active.RemoveAt(i);
                continue;
            }

            if (source.isPlaying)
            {
                continue;
            }

            ResetSource(source);
            active.RemoveAt(i);
            idle.Push(source);
        }
    }

    /// <summary>
    /// 停止所有活跃 AudioSource 并归还到空闲栈。
    /// </summary>
    public void StopAll()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            AudioSource source = active[i];
            if (source == null)
            {
                continue;
            }

            source.Stop();
            ResetSource(source);
            idle.Push(source);
        }

        active.Clear();
    }

    /// <summary>
    /// 创建新的池化 AudioSource 实例。
    /// </summary>
    /// <returns>配置完毕的 AudioSource。</returns>
    private AudioSource CreateSource()
    {
        var go = new GameObject("PooledAudioSource");
        go.transform.SetParent(root, false);
        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        go.SetActive(false);
        return source;
    }

    /// <summary>
    /// 重置 AudioSource 到默认状态并停用。
    /// </summary>
    /// <param name="source">要重置的 AudioSource。</param>
    private static void ResetSource(AudioSource source)
    {
        source.Stop();
        source.clip = null;
        source.loop = false;
        source.pitch = 1f;
        source.volume = 1f;
        source.outputAudioMixerGroup = null;
        source.gameObject.SetActive(false);
    }
}
