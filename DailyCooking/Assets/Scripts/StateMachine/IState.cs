using System;

public interface IState<T>
{
    T StateKey { get; }
    void EnterState();
    void ExitState();
    void UpdateState();
    T GetNextState();
    void Dispose(); 
}
