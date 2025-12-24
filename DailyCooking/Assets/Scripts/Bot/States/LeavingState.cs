using UnityEngine;

public class LeavingState : BotState
{
    public LeavingState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        //Debug.Log("Bot is leaving.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walk);

        
        Vector3 exitPoint = new Vector3(3, 0, -3);

        stateMachine.GetBotController().StartNavMesh();
        stateMachine.GetBotController().NavMeshAgent.SetDestination(exitPoint);
    }

    public override void Update()
    {
        if (stateMachine.GetBotController().NavMeshAgent.remainingDistance <= stateMachine.GetBotController().NavMeshAgent.stoppingDistance)
        {
            
            stateMachine.GetBotController().ResetBot();

            BotManager.Instance.ReturnBotToPool(stateMachine.GetBotController().gameObject);
        }
    }
}