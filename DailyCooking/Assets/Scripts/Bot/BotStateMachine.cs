using UnityEngine;
public enum BotStateType
{
    Idle,
    WaitForTable,
    OrderFood,
    WalkToTable,
    WaitingForFood,
    Eating,
    Leaving
}
public class BotStateMachine
{
    private BotCustomerController botController;
    private BotState currentState;

    public BotState CurrentState => currentState;
    public BotStateMachine(BotCustomerController botController)
    {
        this.botController = botController;
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
        if (!botController.IsHost || !botController.IsServer)
            return; 

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