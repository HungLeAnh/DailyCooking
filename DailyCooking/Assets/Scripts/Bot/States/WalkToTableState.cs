using UnityEngine;
using UnityEngine.AI;

public class WalkToTableState : BotState
{
    public WalkToTableState(BotStateMachine stateMachine) : base(stateMachine) { }
    private Transform seatTransform;
    private Table table;
    private int seatIndex;
    private float waitTimer;
    private const float maxWaitTime = 5f;
    private NavMeshPath path = new NavMeshPath();
    public override void Enter()
    {
        //Debug.Log("Bot is walking to the table.");
        table = stateMachine.GetBotController().TargetTable;
        seatIndex = stateMachine.GetBotController().TargetSeatIndex.Value;
        seatTransform = table.GetSeatTransform(seatIndex);

        var destination = NavMeshExtention.FindNearestPointSmart(seatTransform.position, 5f);
        NavMesh.CalculatePath(stateMachine.GetBotController().transform.position,
            destination, NavMesh.AllAreas, path);
        if (path.status == NavMeshPathStatus.PathComplete)
        {        
            stateMachine.GetBotController().StartNavMesh();
            stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walking);
            stateMachine.GetBotController().NavMeshAgent.SetDestination(destination);
        }
        else
        {
            stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
            //Debug.LogWarning("Path is not valid");
            stateMachine.GetBotController().StopNavMesh();
        }
    }
    public override void Update()
    {
        if (path.status == NavMeshPathStatus.PathComplete)
        {
            if (stateMachine.GetBotController().NavMeshAgent.remainingDistance <= stateMachine.GetBotController().NavMeshAgent.stoppingDistance)
            {
                stateMachine.GetBotController().StopNavMesh();
                stateMachine.GetBotController().transform.position = seatTransform.position;
                Transform lookAtTransform = table.GetKitchenObjectFollowTransform(seatIndex);
                stateMachine.GetBotController().transform.LookAt(lookAtTransform);
                stateMachine.GetBotController().SetCurrentStateServerRpc(BotStateType.OrderFood);
            }

        }
        else
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= maxWaitTime)
            {
                var destination = NavMeshExtention.FindNearestPointSmart(seatTransform.position, 5f);
                NavMesh.CalculatePath(stateMachine.GetBotController().transform.position,
                    destination, NavMesh.AllAreas, path);
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    stateMachine.GetBotController().StartNavMesh();
                    stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walking);
                    stateMachine.GetBotController().NavMeshAgent.SetDestination(destination);
                }
                else
                {
                    stateMachine.GetBotController().Leave();
                }
            }
        }
    }
}
