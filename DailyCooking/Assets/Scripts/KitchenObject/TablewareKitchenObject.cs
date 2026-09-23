using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

public class TablewareKitchenObject : KitchenObject, IInteractable,IHighlightable
{
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO KitchenObjectSO;
    }
    public event EventHandler OnEaten;
    public event EventHandler OnServed;

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    [SerializeField] private GameObject[] visualGameObjectArray;
    [SerializeField] private GameObject[] tablewareGameObjectArray;
    [SerializeField] private GameObject[] eatenGameObjectArray;

    // Replicated plate state, so late joiners see the same plate.
    private NetworkList<FixedString64Bytes> ingredientGuids;
    private readonly NetworkVariable<bool> isEaten = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<bool> isServed = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<KitchenObjectSO> _ingredientSOList = new List<KitchenObjectSO>();

    // Server only: what the customer paid, collected when a player picks up the eaten plate.
    private int cash;
    private int exp;

    public bool IsEaten => isEaten.Value;
    public bool IsServed => isServed.Value;

    protected override void Awake()
    {
        base.Awake();
        ingredientGuids = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        ingredientGuids.OnListChanged += IngredientGuids_OnListChanged;
        isEaten.OnValueChanged += IsEaten_OnValueChanged;
        isServed.OnValueChanged += IsServed_OnValueChanged;

        // Late join: rebuild the local list from the replicated one.
        _ingredientSOList.Clear();
        foreach (FixedString64Bytes guid in ingredientGuids)
            AddLocalIngredient(guid.ToString());
        if (isEaten.Value)
            ApplyEatenVisual();
    }

    public override void OnNetworkDespawn()
    {
        ingredientGuids.OnListChanged -= IngredientGuids_OnListChanged;
        isEaten.OnValueChanged -= IsEaten_OnValueChanged;
        isServed.OnValueChanged -= IsServed_OnValueChanged;
        base.OnNetworkDespawn();
    }

    // Server only. Returns true when the ingredient was added to the plate.
    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        if (!IsServer || kitchenObjectSO == null)
            return false;
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO) || _ingredientSOList.Contains(kitchenObjectSO))
            return false;
        ingredientGuids.Add(kitchenObjectSO.Guid);
        return true;
    }

    private void IngredientGuids_OnListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        if (changeEvent.Type == NetworkListEvent<FixedString64Bytes>.EventType.Add)
            AddLocalIngredient(changeEvent.Value.ToString());
    }

    private void AddLocalIngredient(string kitchenObjectSOGuid)
    {
        KitchenObjectSO kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuid);
        if (kitchenObjectSO == null || _ingredientSOList.Contains(kitchenObjectSO)) return;
        _ingredientSOList.Add(kitchenObjectSO);
        OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
        {
            KitchenObjectSO = kitchenObjectSO,
        });
    }
    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return _ingredientSOList;
    }

    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!isEaten.Value || playerStateMachine.HasKitchenObject())
            return;

        if (SetKitchenObjectParent(playerStateMachine) && (cash > 0 || exp > 0))
        {
            KitchenGameManager.Instance.CollectCash(cash, exp);
            cash = 0;
            exp = 0;
        }
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
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
        if(visualGameObjectArray == null)
            return;
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void Hide()
    {
        if(visualGameObjectArray == null)
            return;
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }

    // Server only: the customer finished eating and paid cash/exp.
    public void SetEaten(int cash, int exp)
    {
        if (!IsServer)
            return;
        this.cash = cash;
        this.exp = exp;
        isEaten.Value = true;
    }

    private void IsEaten_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
            ApplyEatenVisual();
    }

    private void ApplyEatenVisual()
    {
        foreach (var visualGameObject in tablewareGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
        foreach (var eatenGameObject in eatenGameObjectArray)
        {
            eatenGameObject.SetActive(true);
        }
        OnEaten?.Invoke(this, EventArgs.Empty);
    }

    // Server only.
    public void Serve()
    {
        if (IsServer)
            isServed.Value = true;
    }

    private void IsServed_OnValueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
            OnServed?.Invoke(this, EventArgs.Empty);
    }
}
