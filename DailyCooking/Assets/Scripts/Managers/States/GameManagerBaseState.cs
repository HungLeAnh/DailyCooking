public abstract class GameManagerBaseState
{
    protected GameManager gameManager;

    public GameManagerBaseState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}
