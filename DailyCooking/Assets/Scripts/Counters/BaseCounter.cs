using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounter : MonoBehaviour, IKitchenObjectParent
{
    public static event EventHandler<KitchenObjectSO> OnAnyObjectPlacedHere;
    public static event EventHandler<TablewareKitchenObject> OnShowFoodOptionMenu;
    public static event EventHandler<OnShowOptionalMenuArgs> OnShowOptionalMenu;
    public class OnShowOptionalMenuArgs : EventArgs
    {
        public List<KitchenObjectSO> optionalList;
    }
    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
        OnShowFoodOptionMenu = null;
    }

    [SerializeField] private Transform counterTopPoint;

    private KitchenObject _kitchenObject;

    public Transform GetKitchenObjectFollowTransform()
    {
        return counterTopPoint;
    }
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this._kitchenObject = kitchenObject;

        if (kitchenObject != null)
        {
            if (kitchenObject.GetKitchenObjectOptionalProcessSO() != null)
            {
                OnAnyObjectPlacedHere?.Invoke(this, kitchenObject.GetKitchenObjectSO());
            }
            else
            {

            }
        }
    }
    public KitchenObject GetKitchenObject()
    {
        return _kitchenObject;
    }
    public void ClearKitchenObject()
    {
        _kitchenObject = null;
    }
    public bool HasKitchenObject()
    {
        return _kitchenObject != null;
    }
    public virtual void Interact(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.Interact();");
    }
    public virtual void InteractAlternate(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.InteractAlternate();");
    }
    public void FireOnShowFoodOption(TablewareKitchenObject tablewareObject)
    {
        OnShowFoodOptionMenu?.Invoke(this, tablewareObject);
    }
    public void FireOnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        OnShowOptionalMenu?.Invoke(this, new OnShowOptionalMenuArgs
        {
            optionalList = kitchenObjectSOList
        });
    }
}
public interface IHasOptionalSO
{
    public void SetOptionKitchenObjectSO(int index);

}