using UnityEngine;

public class WalkToTableState : BotState
{
    public WalkToTableState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is walking to the table.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walk);

        Table table = stateMachine.GetBotController().TargetTable;
        int seatIndex = stateMachine.GetBotController().TargetSeatIndex;
        Transform seatTransform = table.GetSeatTransform(seatIndex);

        Debug.Log("Bot is walking to seat " + seatIndex + " at table " + table.name);
        // Here you would add the logic to move the bot to the seatTransform.position
    }

    public override void Update()
    {
        // Logic for walking to the table
        // When the bot reaches the table, transition to OrderFoodState
        // For now, let's assume the bot reaches the table instantly
        stateMachine.SetState(new OrderFoodState(stateMachine));
    }
}