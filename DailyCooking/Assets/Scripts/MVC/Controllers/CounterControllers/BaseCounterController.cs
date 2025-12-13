using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounterController : MonoBehaviour, IKitchenObjectParent, IInteractable, IDestroyable,
    IPlaceable
{

    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    private Action onDestroySelf;
    private KitchenObject _kitchenObject;
    private bool isPlaced = false;
    public KitchenObject KitchenObject
    {
        get => _kitchenObject;
        private set
        {
            _kitchenObject = value;
        }
    }

    public Action OnDestroySelf { get =>onDestroySelf; set => onDestroySelf += value; }
    public bool IsPlaced { get => isPlaced; set => isPlaced = value; }

    protected virtual void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    protected virtual void OnDestroy()
    {
        if (KitchenGameManager.Instance == null)
            return;
        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsEditing())
        {
            Hide();
            OnRestartGame(this, PlayerStateMachine.Instance);
        }
    }
    protected virtual void OnRestartGame(object sender, PlayerStateMachine e)
    {
        if (KitchenObject != null)
            KitchenObject.DestroySelf();

        ClearKitchenObject();
    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        return KitchenObject;
    }
    public void ClearKitchenObject(int index = 0)
    {
        KitchenObject = null;
    }
    public bool HasKitchenObject(int index = 0)
    {
        return KitchenObject != null;
    }
    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        return counterTopPoint;
    }
    public void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        KitchenObject = kitchenObject;

        if (kitchenObject != null && kitchenObject.GetKitchenObjectOptionalProcessSO() != null)
        {
            UIPopupManager.Instance.ShowPopup(
                UIPopupType.UIOptionMenuPopup,
                new UIOptionMenuPopup.Param
                {
                    sender = this,
                    objectSO = kitchenObject.GetKitchenObjectSO(),
                }
            );
        }
    }
    public virtual void InteractEvent(PlayerStateMachine playerStateMachine)
    {
    }

    public virtual void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
    }
    public void OnSelected()
    {
        Show();
    }

    public void OnDeselected()
    {
        Hide();
    }

    public void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }

    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
    }
}
