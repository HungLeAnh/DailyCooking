public interface IInteractable
{
    public void InteractEvent(PlayerStateMachine playerStateMachine);
    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine);
}
public interface IHighlightable
{
    public void OnSelected();
    public void OnDeselected();
}