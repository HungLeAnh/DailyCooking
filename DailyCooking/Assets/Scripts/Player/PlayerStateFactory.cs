using System.Collections.Generic;

public class PlayerStateFactory
{
    public Dictionary<PlayerStateMachine.EPlayerState, IState<PlayerStateMachine.EPlayerState>> CreateStates(PlayerStateMachine playerStateMachine)
    {
        var states = new Dictionary<PlayerStateMachine.EPlayerState, IState<PlayerStateMachine.EPlayerState>>
        {
            { PlayerStateMachine.EPlayerState.Idle, new PlayerIdleState(PlayerStateMachine.EPlayerState.Idle) },
            { PlayerStateMachine.EPlayerState.Holding, new PlayerHoldingState(PlayerStateMachine.EPlayerState.Holding) }
        };

        foreach (var key in states.Keys)
        {
            (states[key] as PlayerBaseState).IntializeStates(playerStateMachine.Context);
        }

        return states;
    }
}

