public interface IInteractable
{
    public void FireInteractEvent(PlayerStateMachine playerStateMachine);
    public void FireInteractAlternateEvent(PlayerStateMachine playerStateMachine);
}
