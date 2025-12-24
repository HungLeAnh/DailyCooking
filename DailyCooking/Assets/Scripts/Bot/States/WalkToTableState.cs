using UnityEngine;

public class WalkToTableState : BotState
{
    public WalkToTableState(BotStateMachine stateMachine) : base(stateMachine) { }
    private Transform seatTransform;
    private Table table;
    private int seatIndex;
    public override void Enter()
    {
        //Debug.Log("Bot is walking to the table.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walk);

        table = stateMachine.GetBotController().TargetTable;
        seatIndex = stateMachine.GetBotController().TargetSeatIndex;
        seatTransform = table.GetSeatTransform(seatIndex);

        //Debug.Log("Bot is walking to seat " + seatIndex + " at table " + table.name);
        stateMachine.GetBotController().StartNavMesh();
        stateMachine.GetBotController().NavMeshAgent.SetDestination(seatTransform.position);
    }

    public override void Update()
    {
        // Logic for walking to the table
        // When the bot reaches the table, transition to OrderFoodState
        if (stateMachine.GetBotController().NavMeshAgent.remainingDistance <= stateMachine.GetBotController().NavMeshAgent.stoppingDistance)
        {
            stateMachine.GetBotController().StopNavMesh();
            stateMachine.GetBotController().transform.position = seatTransform.position;
            Transform lookAtTransform = table.GetKitchenObjectFollowTransform(seatIndex);
            stateMachine.GetBotController().transform.LookAt(lookAtTransform);

            stateMachine.SetState(new OrderFoodState(stateMachine));
        }
    }
}
