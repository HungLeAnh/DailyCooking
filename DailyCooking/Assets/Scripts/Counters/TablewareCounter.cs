using System;
using System.Collections.Generic;
using UnityEngine;

public class TablewareCounter : BaseCounter, IHasOptionalSO
{
    public event EventHandler OnTablewareSpawned;
    public event EventHandler OnTablewareRemoved;

    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    private float _spawnTimer;
    private float _spawnTimerMax = 4f;

    private int _tablewareSpawnAmount;
    private int _tablewareSpawnAmountMax = 4;
    private TablewareKitchenObject _lastTablewareKitchenObject;
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
                TablewareKitchenObject tablewareObject = (TablewareKitchenObject)KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
                _lastTablewareKitchenObject = tablewareObject;
                FireOnShowFoodOption(tablewareObject);
                OnTablewareRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (_lastTablewareKitchenObject == null)
            return;
        var foodSO = _lastTablewareKitchenObject.TablewareFoodSOList[index];
        _lastTablewareKitchenObject.SetFoodSO(foodSO);
        _lastTablewareKitchenObject = null;
    }
}
