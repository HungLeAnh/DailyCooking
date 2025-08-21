public abstract class BotState
{
    protected BotStateMachine stateMachine;

    public BotState(BotStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
}