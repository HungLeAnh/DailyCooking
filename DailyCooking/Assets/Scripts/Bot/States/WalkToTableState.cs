using UnityEngine;

public class WalkToTableState : BotState
{
    public WalkToTableState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is walking to the table.");
    }

    public override void Update()
    {
        // Logic for walking to the table
        // When the bot reaches the table, transition to OrderFoodState
        // stateMachine.SetState(new OrderFoodState(stateMachine));
    }
}