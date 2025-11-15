using UnityEngine;

public class InGameState : GameManagerBaseState
{
    public InGameState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        if (!gameManager.GameData.TutorialData.HasPlayedFirstTime)
        {
            TutorialManager.Instance.ShowFirstTimeTutorial();
        }
        GameManager.Instance.InitializePlayer();

    }
}
