using System;
using UnityEngine;
[Serializable]
public class TablewareCounterController : BaseCounterController
{
    public event EventHandler OnTablewareSpawned;
    public event EventHandler OnTablewareRemoved;

    private float _spawnTimer;
    private float _spawnTimerMax = 4f;

    private int _tablewareSpawnAmount;
    private int _tablewareSpawnAmountMax = 4;

    private TablewareCounterView _view;
    private TablewareCounterModel _model;

    public TablewareCounterController(TablewareCounterView view,TablewareCounterModel model) : base(view,model)
    {
        _view = view;
        _model = model;
    }

    private void Update()
    {
        if (_tablewareSpawnAmount < _tablewareSpawnAmountMax)
        {
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer > _spawnTimerMax)
            {
                _spawnTimer = 0f;
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
                KitchenObject.SpawnKitchenObject(_view.TablewareKitchenObjectSO, playerStateMachine);
                OnTablewareRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}