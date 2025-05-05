using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState<T> where T : Enum 
{
    protected Dictionary<T, BaseState<T>> _subStates = new Dictionary<T, BaseState<T>>();
    protected BaseState<T> _currentSubState;
    protected bool IsTransitionongState = false;
    protected bool _isInited = false;
    public BaseState(T key)
    {
        StateKey = key;
    }
    public T StateKey { get; private set; }
    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract T GetNextState();

    public void UpdateSubState()
    {
        if(_currentSubState == null)
            return;
        T nextStateKey = _currentSubState.GetNextState();
        if (!IsTransitionongState && nextStateKey.Equals(_currentSubState.StateKey))
        {
            _currentSubState.UpdateState();
        }
        else if (!IsTransitionongState)
        {
            TransitionToState(nextStateKey);
        }
    }

    private void TransitionToState(T nextStateKey)
    {
        if (_subStates.Count == 0)
            return;
        IsTransitionongState = true;
        _currentSubState.ExitState();
        _currentSubState = _subStates[nextStateKey];
        _currentSubState.EnterState();
        IsTransitionongState = false;
    }
    public virtual void IntializeStates()
    {

    }

    public virtual void OnTriggerEnter(Collider other)
    {
        _currentSubState.OnTriggerEnter(other);
    }
    public virtual void OnTriggerExit(Collider other)
    {
        _currentSubState.OnTriggerExit(other);
    }
    public virtual void OnTriggerStay(Collider other)
    {
        _currentSubState.OnTriggerStay(other);
    }
}
