using UnityEngine;
using UnityEngine.AI;

public class WaitForTableState : BotState
{
    private float roamTimer;
    private float roamInterval = 1f;
    private float checkTableRange = 10f;
    private bool isWalking;

    private Vector3 zeroPos = new Vector3(-3, 0, -3);
    private Vector3 lastOuterPointVisited = Vector3.zero;
    private const float threshold = 2f;

    public WaitForTableState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        //Debug.Log("Bot is waiting for a table.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
        roamTimer = Random.Range(0f, 1f);
        
        // Initialize lastOuterPointVisited to RoamPosX so it starts by going to RoamPosZ (or vice versa)
        lastOuterPointVisited = stateMachine.GetBotController().RoamPosX;
    }

    public override void Update()
    {
        // 1. Check for table within range while roaming
        Table availableTable = TableManager.Instance.GetAvailableTable();
        if (availableTable != null)
        {
            PlacedObjectView availablePlacedObject = availableTable.GetComponent<PlacedObjectView>();
            if (availablePlacedObject != null && !availablePlacedObject.IsPreview.Value)
            {
                int seatIndex = availableTable.GetAvailableSeat();
                if (seatIndex != -1)
                {
                    stateMachine.GetBotController().SetSeatServerRpc(availableTable, seatIndex);
                    availableTable.OccupySeatServerRpc(seatIndex);
                    stateMachine.GetBotController().SetCurrentStateServerRpc(BotStateType.WalkToTable);
                    return;
                }
            }
        }

        // 2. Roaming logic
        if (isWalking)
        {
            if (!stateMachine.GetBotController().NavMeshAgent.pathPending && 
                stateMachine.GetBotController().NavMeshAgent.remainingDistance <= stateMachine.GetBotController().NavMeshAgent.stoppingDistance)
            {
                isWalking = false;
                
                // Track if we just reached an outer point
                Vector3 destination = stateMachine.GetBotController().NavMeshAgent.destination;
                if (Vector3.Distance(destination, stateMachine.GetBotController().RoamPosX) < threshold ||
                    Vector3.Distance(destination, stateMachine.GetBotController().RoamPosZ) < threshold)
                {
                    lastOuterPointVisited = destination;
                }

                stateMachine.GetBotController().StopNavMesh();
                stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
                if(Vector3.Distance(destination, zeroPos) < 0.5f)
                    roamTimer = 0;
                else
                    roamTimer = roamInterval;
            }
        }
        else
        {
            roamTimer -= Time.deltaTime;
            if (roamTimer <= 0f)
            {
                Roam();
            }
        }
    }

    private void Roam()
    {
        Vector3 currentPos = stateMachine.GetBotController().transform.position;
        Vector3 targetPos = zeroPos;

        // Decision logic based on current location
        if (Vector3.Distance(currentPos, stateMachine.GetBotController().RoamPosX) < threshold ||
            Vector3.Distance(currentPos, stateMachine.GetBotController().RoamPosZ) < threshold)
        {
            // If at outer point (X or Z), always return to ZeroPos
            targetPos = zeroPos;
        }
        else
        {
            // If at ZeroPos (or anywhere else), go to the outer point we DIDN'T visit last
            if (Vector3.Distance(lastOuterPointVisited, stateMachine.GetBotController().RoamPosX) < threshold)
            {
                targetPos = stateMachine.GetBotController().RoamPosZ;
            }
            else
            {
                targetPos = stateMachine.GetBotController().RoamPosX;
            }
        }

        stateMachine.GetBotController().StartNavMesh();
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Walking);
        stateMachine.GetBotController().NavMeshAgent.SetDestination(targetPos);
        isWalking = true;
    }
}
