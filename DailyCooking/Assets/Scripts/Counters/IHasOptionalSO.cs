using System.Collections.Generic;

public interface IHasOptionalSO
{
    public void SetOptionKitchenObjectSO(int index);
    public virtual List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return null;
    }
    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList);

}