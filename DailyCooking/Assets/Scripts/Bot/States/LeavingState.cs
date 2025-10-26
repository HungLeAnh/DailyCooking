using UnityEngine;

public class LeavingState : BotState
{
    public LeavingState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is leaving.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walk);

        // TODO: Determine the exit point
        Vector3 exitPoint = new Vector3(10, 0, 0); // Placeholder
        stateMachine.GetBotController().NavMeshAgent.SetDestination(exitPoint);
        stateMachine.GetBotController().ClearTargetTable();
    }

    public override void Update()
    {
        if (stateMachine.GetBotController().NavMeshAgent.remainingDistance <= stateMachine.GetBotController().NavMeshAgent.stoppingDistance)
        {
            // Bot has reached the exit, destroy it
            stateMachine.GetBotController().ResetBot();

            BotManager.Instance.ReturnBotToPool(stateMachine.GetBotController().gameObject);
        }
    }
}