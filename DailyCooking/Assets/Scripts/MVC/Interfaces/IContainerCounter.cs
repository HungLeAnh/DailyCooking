using System.Collections.Generic;

public interface IContainerCounter
{
    public abstract List<KitchenObjectSO> GetContainerKitchenObjectType();
    public abstract void Refill(float fillAmount, string kitchenObjectGuid);
}
