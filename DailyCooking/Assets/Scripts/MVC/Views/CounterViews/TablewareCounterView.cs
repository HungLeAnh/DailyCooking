using System;
using System.Collections.Generic;
using UnityEngine;
public class TablewareCounterView : BaseCounterView
{
    public event EventHandler OnTablewareSpawned;
    public event EventHandler OnTablewareRemoved;

    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;
    [SerializeField] private float _spawnTimerMax = 4f;
    [SerializeField] private int _tablewareSpawnAmountMax = 4;

    public KitchenObjectSO TablewareKitchenObjectSO { get => _tablewareKitchenObjectSO; set => _tablewareKitchenObjectSO = value; }
    public int TablewareSpawnAmountMax { get => _tablewareSpawnAmountMax; set => _tablewareSpawnAmountMax = value; }
    public float SpawnTimerMax { get => _spawnTimerMax; set => _spawnTimerMax = value; }

    public override object CreateControllerFromView()
    {
        return new TablewareCounterController(this,new TablewareCounterModel());
    }
    
    public void FireOnTablewareSpawned()
    {
        OnTablewareSpawned?.Invoke(this, EventArgs.Empty);
    }
    public void FireOnTablewareRemoved()
    {
        OnTablewareRemoved?.Invoke(this, EventArgs.Empty);
    }
}