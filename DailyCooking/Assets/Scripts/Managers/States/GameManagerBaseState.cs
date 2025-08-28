public abstract class GameManagerBaseState
{
    protected GameManager gameManager;

    public GameManagerBaseState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
