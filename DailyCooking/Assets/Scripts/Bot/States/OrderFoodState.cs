using System;
using UnityEngine;

public class OrderFoodState : BotState
{
    public OrderFoodState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is ordering food.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Talk);
        stateMachine.GetBotController().OnInteract += OnOrderComplete;
        stateMachine.GetBotController().ShowOrder();
    }

    private void OnOrderComplete(PlayerStateMachine playerStateMachine)
    {
        stateMachine.GetBotController().OrderFood();
        stateMachine.SetState(new WaitingForFoodState(stateMachine));
    }

    public override void Update()
    {

    }
}