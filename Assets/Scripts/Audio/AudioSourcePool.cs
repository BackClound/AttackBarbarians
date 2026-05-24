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

    public int ActiveCount => active.Count;

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
