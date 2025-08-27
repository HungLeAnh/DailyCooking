using UnityEngine;
using UnityEngine.AI;

public class BotController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    private BotStateMachine stateMachine;

    public Table TargetTable { get; set; }
    public int TargetSeatIndex { get; set; }

    public NavMeshAgent NavMeshAgent { get => navMeshAgent; }

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