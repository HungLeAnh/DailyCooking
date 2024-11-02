using System;
using System.Collections.Generic;
using UnityEngine;

public class TablewareCounter : BaseCounter
{
    public event EventHandler OnTablewareSpawned;
    public event EventHandler OnTablewareRemoved;

    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    private float _spawnTimer;
    private float _spawnTimerMax = 4f;

    private int _tablewareSpawnAmount;
    private int _tablewareSpawnAmountMax = 4;


    private void Update()
    {
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer > _spawnTimerMax)
        {
            _spawnTimer = 0f;

            if (_tablewareSpawnAmount < _tablewareSpawnAmountMax)
            {
                _tablewareSpawnAmount++;
                OnTablewareSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is empty handed
            if (_tablewareSpawnAmount > 0)
            {
                //There is at least one tableware
                _tablewareSpawnAmount--;

                KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);

                OnTablewareRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
public class TablewareCounterVisual : MonoBehaviour
{
    [SerializeField] private TablewareCounter _tablewareCounter;
    [SerializeField] private Transform _tablewareVisualPrefab;

    private List<GameObject> _tablewareVisualGameObjectList;

    private void Awake()
    {
        _tablewareVisualGameObjectList = new List<GameObject>();
    }

    private void Start()
    {
        _tablewareCounter.OnTablewareSpawned += TablewareCounter_OnTablewareSpawned;
        _tablewareCounter.OnTablewareRemoved += TablewareCounter_OnTablewareRemoved;
    }

    private void TablewareCounter_OnTablewareRemoved(object sender, System.EventArgs e)
    {
        GameObject tablewareGameObject = _tablewareVisualGameObjectList[_tablewareVisualGameObjectList.Count - 1];
        _tablewareVisualGameObjectList.Remove(tablewareGameObject);
        Destroy(tablewareGameObject);
    }

    private void TablewareCounter_OnTablewareSpawned(object sender, System.EventArgs e)
    {
        Transform tablewareVisualTransform = Instantiate(_tablewareVisualPrefab, _tablewareCounter.GetKitchenObjectFollowTransform());
        float tablewareOffsetY = .1f;
        tablewareVisualTransform.localPosition = new Vector3(0, tablewareOffsetY * _tablewareVisualGameObjectList.Count, 0);
        _tablewareVisualGameObjectList.Add(tablewareVisualTransform.gameObject);
    }
}
public class FoodCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct KitchenObjectSO_GameObject
    {
        public GameObject GameObject;
        public KitchenObjectSO kitchenObjectSO;
    }

    [SerializeField] private TablewareKitchenObject tablewareKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> KitchenObjectSO_GameObjectList;
    private void Start()
    {
        tablewareKitchenObject.OnIngredientAdded += TablewareKitchenObject_OnIngredientAdded;
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            kitchenObjectSOGameObject.GameObject.SetActive(false);
        }
    }

    private void TablewareKitchenObject_OnIngredientAdded(object sender, TablewareKitchenObject.OnIngredientAddedEventArgs e)
    {
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in KitchenObjectSO_GameObjectList)
        {
            if (kitchenObjectSOGameObject.kitchenObjectSO == e.KitchenObjectSO)
            {
                kitchenObjectSOGameObject.GameObject.SetActive(true);
            }
        }
    }
}
