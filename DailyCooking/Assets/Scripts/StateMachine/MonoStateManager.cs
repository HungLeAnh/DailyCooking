using System;
using System.Collections.Generic;
using UnityEngine;

public class MonoStateManager<T> : MonoBehaviour where T : Enum
{
    private StateManager<T> _stateManager = new StateManager<T>();

    public void SetStates(Dictionary<T, IState<T>> states, T startingState)
    {
        _stateManager.SetStates(states, startingState);
    }

    private void Start()
    {
        _stateManager.Start();
    }

    private void Update()
    {
        _stateManager.Update();
    }

    public void Dispose()
    {
        _stateManager.Dispose();
    }
}

