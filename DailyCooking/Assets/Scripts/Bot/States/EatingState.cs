using UnityEngine;

public class EatingState : BotState
{
    private float eatingTimer;

    public EatingState(BotStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Bot is eating.");
        stateMachine.GetBotController().PlayAnimation(BotAnimation.State.Eat);
        eatingTimer = 5f; // Eat for 5 seconds
    }

    public override void Update()
    {
        eatingTimer -= Time.deltaTime;
        if (eatingTimer <= 0)
        {
            stateMachine.SetState(new LeavingState(stateMachine));
            stateMachine.GetBotController().FinishEating();
        }
    }
}