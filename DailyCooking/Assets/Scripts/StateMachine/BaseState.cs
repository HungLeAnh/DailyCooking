using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState<T> : IState<T>, IDisposable where T : Enum 
{
    protected SubStateMachine<T> _subStateMachine = new SubStateMachine<T>();
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

    public virtual void IntializeStates()
    {

    }

    public virtual void Dispose()
    {
        _subStateMachine.Dispose();
    }
}
