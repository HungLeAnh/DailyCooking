using UnityEngine;

public class LeavingState : BotState
{
    public LeavingState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is leaving.");
    }

    public override void Update()
    {
        // Logic for leaving
        // After leaving, the bot can be destroyed or reset
    }
}