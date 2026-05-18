using System;
using Unity.Netcode;
using UnityEngine;

public class BaseCounterController : NetworkBehaviour, IKitchenObjectParent, IInteractable, IDestroyable,
    IPlaceable, IModuleItem,IHighlightable
{

    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private GameObject[] visualGameObjectArray;

    private Action onDestroySelf;
    private KitchenObject _kitchenObject;
    public KitchenObject KitchenObject
    {
        get => _kitchenObject;
        private set
        {
            _kitchenObject = value;
        }
    }

    public Action OnDestroySelf { get =>onDestroySelf; set => onDestroySelf += value; }

    protected virtual void Start()
    {
        //KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    protected virtual void OnDestroy()
    {
        if (KitchenGameManager.Instance == null)
            return;
        //KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsEditing())
        {
            Hide();
            OnRestartGame(this);
        }
    }
    protected virtual void OnRestartGame(object sender)
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
                    Title = "Select way to process ingredient:"
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
        NetworkObject.Despawn();
        Destroy(this);
    }

    public virtual bool CanRemove()
    {
        return !HasKitchenObject();
    }

    public void RegisterItem()
    {
        CounterModules.Instance.AddController(this);
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
