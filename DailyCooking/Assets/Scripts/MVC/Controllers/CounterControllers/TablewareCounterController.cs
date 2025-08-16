
using System;
using UnityEngine;

public class TablewareCounterController : BaseCounterController
{
    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    [SerializeField] private float _spawnTimerMax = 4f;
    [SerializeField] private int _tablewareSpawnAmountMax = 4;

    private TablewareCounterModel _tablewareCounterModel;
    private TablewareCounterService _tablewareCounterService;
    private TablewareCounterView _tablewareCounterView;

    private void Awake()
    {
        _tablewareCounterModel = new TablewareCounterModel();
        BaseCounterModel = _tablewareCounterModel;
        _tablewareCounterService = new TablewareCounterService(_spawnTimerMax, _tablewareSpawnAmountMax, _tablewareKitchenObjectSO);
        _tablewareCounterView = (TablewareCounterView)BaseCounterView;

        _tablewareCounterService.OnTablewareSpawned += () => _tablewareCounterView.OnTablewareSpawned();
        _tablewareCounterService.OnTablewareRemoved += () => _tablewareCounterView.OnTablewareRemoved();
    }

    private void Update()
    {
        _tablewareCounterService.Update(_tablewareCounterModel);
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _tablewareCounterService.Interact(_tablewareCounterModel, playerStateMachine);
    }
}
