
using System;
using System.Collections.Generic;
using UnityEngine;

public class TablewareCounterController : BaseCounterController
{
    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    [SerializeField] private float _spawnTimerMax = 4f;
    [SerializeField] private int _tablewareSpawnAmountMax = 4;
    [SerializeField] private Transform _tablewareVisualPrefab;
    [SerializeField] float tablewareOffsetX = .1f;
    [SerializeField] float tablewareOffsetY = 0.5f;
    [SerializeField] Vector3 tablewareRotation = Vector3.zero;

    private List<GameObject> _tablewareVisualGameObjectList;
    private float _spawnTimer;
    private int _tablewareSpawnAmount;

    public int TablewareSpawnAmount { get => _tablewareSpawnAmount; set => _tablewareSpawnAmount = value; }
    public float SpawnTimer { get => _spawnTimer; set => _spawnTimer = value; }

    private void Awake()
    {
        _tablewareVisualGameObjectList = new List<GameObject>();
    }

    private void Update()
    {
        if (TablewareSpawnAmount < _tablewareSpawnAmountMax)
        {
            SpawnTimer += Time.deltaTime;
            if (SpawnTimer >= _spawnTimerMax)
            {
                SpawnTimer = 0f;
                TablewareSpawnAmount++;
                OnTablewareSpawned();
            }
        }
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (TablewareSpawnAmount > 0)
            {
                TablewareSpawnAmount--;
                KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
                OnTablewareRemoved();
            }
        }
    }

    public void OnTablewareRemoved()
    {
        if (_tablewareVisualGameObjectList == null ||
            _tablewareVisualGameObjectList.Count < 0)
            return;

        GameObject tablewareGameObject = _tablewareVisualGameObjectList[_tablewareVisualGameObjectList.Count - 1];
        _tablewareVisualGameObjectList.Remove(tablewareGameObject);
        Destroy(tablewareGameObject);
    }

    public void OnTablewareSpawned()
    {
        Transform tablewareVisualTransform = Instantiate(_tablewareVisualPrefab, GetKitchenObjectFollowTransform());

        tablewareVisualTransform.localPosition = new Vector3(tablewareOffsetX * _tablewareVisualGameObjectList.Count, tablewareOffsetY, 0);
        tablewareVisualTransform.Rotate(tablewareRotation);
        _tablewareVisualGameObjectList.Add(tablewareVisualTransform.gameObject);
    }
}
