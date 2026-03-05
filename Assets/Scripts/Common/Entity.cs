using UnityEngine;

public class Entity : MonoBehaviour
{
    public Animator anim { get; private set; }
    public virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }
}
