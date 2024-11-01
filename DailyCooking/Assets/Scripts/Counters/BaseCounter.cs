using System;
using UnityEngine;

public class BaseCounter : MonoBehaviour, IKitchenObjectParent
{
    public static event EventHandler OnAnyObjectPlacedHere;

    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
    }

    public event EventHandler<OnKitchenObjectPlacedHereArgs> OnKitchenObjectPlacedHere;
    public class OnKitchenObjectPlacedHereArgs: EventArgs
    {
        public KitchenObject kitchenObject;
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
                OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
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
    public virtual void SetOptionKitchenObjectSO(KitchenObjectSO kitchenObjectSO) { }

    public virtual void Interact(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.Interact();");
    }
    public virtual void InteractAlternate(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.InteractAlternate();");

    }
}
