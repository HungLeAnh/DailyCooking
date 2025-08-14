using System;

public interface IHasProgress
{
    public abstract bool IsDone();
    public abstract float GetProgress();
}