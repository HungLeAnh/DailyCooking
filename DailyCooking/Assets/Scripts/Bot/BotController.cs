using UnityEngine;

public class BotController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private BotStateMachine stateMachine;

    public Animator Animator { get => animator; }

    private void Awake()
    {
        stateMachine = new BotStateMachine(this);
    }

    public void PlayAnimation(BotAnimation.State animationState)
    {
        animator.Play(animationState.ToString());
    }

    private void Update()
    {
        stateMachine.Update();
    }
}