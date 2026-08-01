using System;
using UnityEngine;

public class OrderFoodState : BotState
{
    public OrderFoodState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        //Debug.Log("Bot is ordering food.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Talk);
        stateMachine.GetBotController().OnInteract += OnOrderComplete;
        stateMachine.GetBotController().ShowOrder();
    }

    private void OnOrderComplete(PlayerStateMachine playerStateMachine)
    {
        stateMachine.GetBotController().OrderFoodServerRpc();
        Debug.Log("Bot has ordered food and is now waiting.");
    }

    public override void Update()
    {

    }
    public override void Exit()
    {
        stateMachine.GetBotController().OnInteract -= OnOrderComplete;
    }
}