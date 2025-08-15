using System;

[Serializable]
public class BaseCounterModel
{
    private KitchenObject _kitchenObject;
    public KitchenObject KitchenObject
    {
        get => _kitchenObject;
        private set
        {
            _kitchenObject = value;
            
        }
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        KitchenObject = kitchenObject;
    }

    public void ClearKitchenObject()
    {
        KitchenObject = null;
    }
}
