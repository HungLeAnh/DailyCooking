using UnityEngine;

public class BotIdleState : BotState
{
    public BotIdleState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is Idling.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
    }

    public override void Update()
    {

    }
}