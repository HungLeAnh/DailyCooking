using System;

[Serializable]
public class BaseCounterModel
{
    private KitchenObject _kitchenObject;
    public KitchenObject KitchenObject
    {
        get => _kitchenObject;
        set
        {
            _kitchenObject = value;
            
        }
    }
}
