using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseCounterController : NetworkBehaviour, IKitchenObjectParent, IInteractable, IDestroyable,
    IPlaceable, IModuleItem,IHighlightable
{

    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private MeshRenderer[] visualGameObjectArray;
    [Tooltip("Additional top slots for multi-cell counters, ordered to match GetGridPositionList() cell order. Empty = single slot (counterTopPoint).")]
    [SerializeField] private List<Transform> topSlots = new List<Transform>();

    private KitchenObject _kitchenObject;
    public KitchenObject KitchenObject
    {
        get => _kitchenObject;
        private set
        {
            _kitchenObject = value;
        }
    }

    public event Action OnDestroySelf;

    protected virtual void Awake()
    {
        if (visualGameObjectArray == null || visualGameObjectArray.Length == 0)
            visualGameObjectArray = GetComponentsInChildren<MeshRenderer>(true);
    }

    protected virtual void Start()
    {
        //KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    public override void OnDestroy()
    {
        highlight?.Release();
        base.OnDestroy();
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
        return GetTopSlotTransform(index) ?? counterTopPoint;
    }

    public virtual int GetTopSlotCount()
    {
        if (topSlots != null && topSlots.Count > 0)
            return topSlots.Count;
        return counterTopPoint != null ? 1 : 0;
    }

    public virtual Transform GetTopSlotTransform(int index)
    {
        if (topSlots != null && topSlots.Count > 0)
        {
            if (index < 0 || index >= topSlots.Count)
                return null;
            return topSlots[index];
        }
        return index == 0 ? counterTopPoint : null;
    }
    // Called on every peer when a kitchen object's replicated parent becomes this counter.
    public void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        KitchenObject = kitchenObject;
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
        Highlight.SetActive(true);
    }

    public void Hide()
    {
        Highlight.SetActive(false);
    }

    private HighlightMaterials highlight;
    private HighlightMaterials Highlight => highlight ??= new HighlightMaterials(visualGameObjectArray);

    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
        NetworkObject.Despawn(true);
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
