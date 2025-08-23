using UnityEngine;

public class WaitForTableState : BotState
{
    public WaitForTableState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is waiting for a table.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
    }

    public override void Update()
    {
        // Logic to check if a table is available
        // If a table is available, transition to WalkToTableState
        // stateMachine.SetState(new WalkToTableState(stateMachine));
    }
}