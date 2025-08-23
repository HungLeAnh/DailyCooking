using UnityEngine;

public class EatingState : BotState
{
    public EatingState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is eating.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Eat);
    }

    public override void Update()
    {
        // Logic for eating
        // After eating, transition to LeavingState
        // stateMachine.SetState(new LeavingState(stateMachine));
    }
}