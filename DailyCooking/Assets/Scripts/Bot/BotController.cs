using UnityEngine;

public class BotController : MonoBehaviour
{
    private BotStateMachine stateMachine;

    private void Awake()
    {
        stateMachine = new BotStateMachine(this);
    }

    private void Update()
    {
        stateMachine.Update();
    }
}