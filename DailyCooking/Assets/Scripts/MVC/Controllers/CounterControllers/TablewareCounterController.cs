
using System;
using UnityEngine;

public class TablewareCounterController : BaseCounterController
{
    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    [SerializeField] private float _spawnTimerMax = 4f;
    [SerializeField] private int _tablewareSpawnAmountMax = 4;

    private TablewareCounterModel _tablewareCounterModel;
    private TablewareCounterView _tablewareCounterView;

    private void Awake()
    {
        _tablewareCounterModel = new TablewareCounterModel();
        BaseCounterModel = _tablewareCounterModel;
        _tablewareCounterView = (TablewareCounterView)BaseCounterView;

    }

    private void Update()
    {
        if (_tablewareCounterModel.TablewareSpawnAmount < _tablewareSpawnAmountMax)
        {
            _tablewareCounterModel.SpawnTimer += Time.deltaTime;
            if (_tablewareCounterModel.SpawnTimer >= _spawnTimerMax)
            {
                _tablewareCounterModel.SpawnTimer = 0f;
                _tablewareCounterModel.TablewareSpawnAmount++;
                _tablewareCounterView.OnTablewareSpawned();
            }
        }
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (_tablewareCounterModel.TablewareSpawnAmount > 0)
            {
                _tablewareCounterModel.TablewareSpawnAmount--;
                KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
                _tablewareCounterView.OnTablewareRemoved();
            }
        }
    }
}
