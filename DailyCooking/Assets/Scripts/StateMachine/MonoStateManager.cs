using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class MonoStateManager<T> : MonoBehaviour where T : Enum
{
    protected Dictionary<T,BaseState<T>> _states = new Dictionary<T, BaseState<T>>();
    protected BaseState<T> _currentState;
    protected bool IsTransitioningState = false;
    private void Start()
    {
        _currentState.EnterState();
    }
    private void Update()
    {
        T nextStateKey = _currentState.GetNextState();
        if (!IsTransitioningState && nextStateKey.Equals(_currentState.StateKey))
        {
            _currentState.UpdateState();
        }
        else if(!IsTransitioningState)
        {
            TransitionToState(nextStateKey);
        }
    }

    private void TransitionToState(T nextStateKey)
    {
        if (_states.Count == 0)
            return;
        IsTransitioningState = true;
        _currentState.ExitState();
        _currentState = _states[nextStateKey];
        _currentState.EnterState();
        IsTransitioningState = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        _currentState.OnTriggerEnter(other);
    }
    private void OnTriggerExit(Collider other)
    {
        _currentState.OnTriggerExit(other);
    }
    private void OnTriggerStay(Collider other)
    {
        _currentState.OnTriggerStay(other);
    }
}
