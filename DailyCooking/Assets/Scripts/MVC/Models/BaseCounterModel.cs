using MVC;
using Observer;
using System;
[Serializable]
public class BaseCounterModel : Observable
{
    private KitchenObject _kitchenObject;

    public KitchenObject KitchenObject { get => _kitchenObject; set => _kitchenObject = value; }
}
