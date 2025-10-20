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
                    player.GetKitchenObject().DestroySelf();
                }
            }
        }

    }

    public override void Update()
    {
        // Logic for waiting for food
        // When the food arrives, transition to EatingState
        // stateMachine.SetState(new EatingState(stateMachine));
    }
}