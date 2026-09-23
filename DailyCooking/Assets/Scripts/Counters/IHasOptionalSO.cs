using Unity.Netcode;

// Something that offers the player a choice through UIOptionMenuPopup. Implementers are
// NetworkBehaviours; the menu is opened with PlayerStateMachine.ShowOptionMenu and the pick
// comes back through PlayerStateMachine.RequestOption.
public interface IHasOptionalSO
{
    // Runs on the server. index refers to the option list that was shown; validate it.
    public void ApplyOption(PlayerStateMachine actor, int index);
}
