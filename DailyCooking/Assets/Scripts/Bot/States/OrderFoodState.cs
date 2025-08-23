using UnityEngine;

public class OrderFoodState : BotState
{
    public OrderFoodState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is ordering food.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Talk);
    }

    public override void Update()
    {
        // Logic for ordering food
        // After ordering, transition to WaitingForFoodState
        // stateMachine.SetState(new WaitingForFoodState(stateMachine));
    }
}