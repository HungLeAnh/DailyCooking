using System;
using System.Collections.Generic;

public class SubStateMachine<T> where T : Enum
{
    private Dictionary<T, IState<T>> _subStates = new Dictionary<T, IState<T>>();
    private IState<T> _currentSubState;
    private bool IsTransitioningState = false;

    public IState<T> CurrentSubState => _currentSubState;
    public void SetStates(Dictionary<T, IState<T>> states, T startingState)
    {
        _subStates = states;
        _currentSubState = _subStates[startingState];
    }

    public void Start()
    {
        _currentSubState.EnterState();
    }

    public void Update()
    {
        if (_currentSubState == null)
            return;

        T nextStateKey = _currentSubState.GetNextState();
        if (!IsTransitioningState && nextStateKey.Equals(_currentSubState.StateKey))
        {
            _currentSubState.UpdateState();
        }
        else if (!IsTransitioningState)
        {
            TransitionToState(nextStateKey);
        }
    }

    public void TransitionToState(T nextStateKey)
    {
        if (_subStates.Count == 0)
            return;

        IsTransitioningState = true;
        _currentSubState.ExitState();
        _currentSubState = _subStates[nextStateKey];
        _currentSubState.EnterState();
        IsTransitioningState = false;
    }

    public void Dispose()
    {
        foreach (var state in _subStates)
        {
            state.Value.Dispose();
        }
        _subStates.Clear();
        _currentSubState = null;
    }
}

