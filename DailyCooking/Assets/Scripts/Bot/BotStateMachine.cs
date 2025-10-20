using UnityEngine;

public class BotStateMachine
{
    private BotCustomerController botController;
    private BotState currentState;

    public BotStateMachine(BotCustomerController botController)
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

    public BotCustomerController GetBotController()
    {
        return botController;
    }
    
}