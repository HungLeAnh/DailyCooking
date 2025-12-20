using UnityEngine;

public class InGameState : GameManagerBaseState
{
    public InGameState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        GameManager.Instance.InitializePlayer();
    }
}
