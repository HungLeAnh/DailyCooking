using UnityEngine;

public class WaitingForFoodState : BotState
{
    public WaitingForFoodState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is waiting for food.");
    }

    public override void Update()
    {
        // Logic for waiting for food
        // When the food arrives, transition to EatingState
        // stateMachine.SetState(new EatingState(stateMachine));
    }
}