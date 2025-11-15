using System;
using UnityEngine;

public class WaitingForFoodState : BotState
{
    public WaitingForFoodState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is waiting for food.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Idle);
        stateMachine.GetBotController().OnInteract += OnFoodServed;

    }

    private void OnFoodServed(PlayerStateMachine playerStateMachine)
    {
        IKitchenObjectParent player = playerStateMachine as IKitchenObjectParent;
        if (player.HasKitchenObject())
        {
            if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (stateMachine.GetBotController().IsServerCorrectFood(tablewareKitchenObject))
                {
                    stateMachine.SetState(new EatingState(stateMachine));
                    stateMachine.GetBotController().StopBubble();

                    player.GetKitchenObject().SetKitchenObjectParent(stateMachine.GetBotController().TargetTable,
                        stateMachine.GetBotController().TargetSeatIndex);
                    tablewareKitchenObject.Serve();
                }
            }
        }

    }

    public override void Update()
    {

    }
    public override void Exit()
    {
        stateMachine.GetBotController().OnInteract -= OnFoodServed;
    }
}