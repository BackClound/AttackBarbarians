using UnityEngine;

public class Entity : MonoBehaviour
{
    public StateMachine stateMachine;
    public Animator anim { get; private set; }
    public Rigidbody2D rb;
    public Collider2D coll;
    public virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
    }

    public virtual void Start() { }

    public virtual void OnAniamtorFinished()
    {
    }
}
