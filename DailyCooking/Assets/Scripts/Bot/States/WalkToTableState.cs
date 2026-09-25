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
        seatTransform = table != null ? table.GetSeatTransform(seatIndex) : null;
        if (seatTransform == null)
        {
            // The table was removed (or never resolved) before the bot started walking.
            stateMachine.GetBotController().Leave();
            return;
        }

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
        if (seatTransform == null)
            return;
        if (path.status == NavMeshPathStatus.PathComplete)
        {
            var agent = stateMachine.GetBotController().NavMeshAgent;
            // remainingDistance reads 0 until the path is computed.
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                stateMachine.GetBotController().StopNavMesh();
                stateMachine.GetBotController().transform.position = seatTransform.position;
                Transform lookAtTransform = table.GetKitchenObjectFollowTransform(seatIndex);
                stateMachine.GetBotController().transform.LookAt(lookAtTransform);
                stateMachine.GetBotController().SetCurrentState(BotStateType.OrderFood);
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
