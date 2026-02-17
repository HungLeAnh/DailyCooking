using System;
using System.Collections.Generic;
using UnityEngine;

public class StateManager<T> where T : Enum
{
    protected Dictionary<T, IState<T>> _states = new Dictionary<T, IState<T>>();
    protected IState<T> _currentState;
    protected bool IsTransitioningState = false;

    public IState<T> CurrentState => _currentState;

    public void SetStates(Dictionary<T, IState<T>> states, T startingState)
    {
        _states = states;
        _currentState = _states[startingState];
    }

    public void Start()
    {
        _currentState.EnterState();
    }

    public void Update()
    {
        T nextStateKey = _currentState.GetNextState();
        if (!IsTransitioningState && nextStateKey.Equals(_currentState.StateKey))
        {
            _currentState.UpdateState();
        }
        else if (!IsTransitioningState)
        {
            TransitionToState(nextStateKey);
        }
    }

    public void TransitionToState(T nextStateKey)
    {
        if (_states.Count == 0)
            return;
        IsTransitioningState = true;
        _currentState.ExitState();
        _currentState = _states[nextStateKey];
        _currentState.EnterState();
        IsTransitioningState = false;
    }

    public void Dispose()
    {
        foreach (var state in _states)
        {
            state.Value.Dispose();
        }
        _states.Clear();
        _currentState = null;
    }
}

