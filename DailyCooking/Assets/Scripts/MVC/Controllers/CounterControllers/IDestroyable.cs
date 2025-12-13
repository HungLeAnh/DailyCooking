using System;

public interface IDestroyable
{
    public Action OnDestroySelf { get ; set; }
    public void DestroySelf();

}