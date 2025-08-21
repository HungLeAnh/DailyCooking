using UnityEngine;

public class BotStateMachine
{
    private BotController botController;
    private BotState currentState;

    public BotStateMachine(BotController botController)
    {
        this.botController = botController;
        // Set initial state
        SetState(new WaitForTableState(this));
    }

    public void SetState(BotState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }

        currentState = newState;
        currentState.Enter();
    }

    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    public BotController GetBotController()
    {
        return botController;
    }
}