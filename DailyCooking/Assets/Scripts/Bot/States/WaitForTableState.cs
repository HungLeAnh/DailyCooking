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
        Table availableTable = TableManager.Instance.GetAvailableTable();
        if (availableTable != null && availableTable.IsPlaced)
        {
            int seatIndex = availableTable.GetAvailableSeat();
            if (seatIndex != -1)
            {
                stateMachine.GetBotController().TargetTable = availableTable;
                stateMachine.GetBotController().TargetSeatIndex = seatIndex;
                availableTable.OccupySeat(seatIndex);
                stateMachine.SetState(new WalkToTableState(stateMachine));
            }
        }
    }
}