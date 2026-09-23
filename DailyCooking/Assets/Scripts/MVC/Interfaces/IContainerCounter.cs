using System.Collections.Generic;

public interface IContainerCounter
{
    public abstract List<KitchenObjectSO> GetContainerKitchenObjectType();
    // Server only. Returns false when the container cannot take this ingredient.
    public abstract bool Refill(float fillAmount, string kitchenObjectGuid);
}
