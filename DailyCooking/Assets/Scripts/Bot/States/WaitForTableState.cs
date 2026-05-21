using UnityEngine;

public class WaitForTableState : BotState
{
    public WaitForTableState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        //Debug.Log("Bot is waiting for a table.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
    }

    public override void Update()
    {
        Table availableTable = TableManager.Instance.GetAvailableTable();
        if (availableTable == null)
            return;
        PlacedObjectView availablePlacedObject = availableTable.GetComponent<PlacedObjectView>();
        if (availableTable != null && availablePlacedObject != null && !availablePlacedObject.IsPreview.Value)
        {
            int seatIndex = availableTable.GetAvailableSeat();
            if (seatIndex != -1)
            {
                stateMachine.GetBotController().SetSeatServerRpc(availableTable, seatIndex);

                availableTable.OccupySeatServerRpc(seatIndex);
                stateMachine.GetBotController().SetCurrentStateServerRpc(BotStateType.WalkToTable);
            }
        }
    }
}