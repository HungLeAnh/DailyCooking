using System;

public interface IDestroyable
{
    event Action OnDestroySelf;
    public void DestroySelf();

}