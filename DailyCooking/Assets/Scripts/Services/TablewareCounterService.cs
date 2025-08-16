using System;
using UnityEngine;

public class TablewareCounterService
{
    public event Action OnTablewareSpawned;
    public event Action OnTablewareRemoved;

    private readonly float _spawnTimerMax;
    private readonly int _tablewareSpawnAmountMax;
    private readonly KitchenObjectSO _tablewareKitchenObjectSO;

    public TablewareCounterService(float spawnTimerMax, int tablewareSpawnAmountMax, KitchenObjectSO tablewareKitchenObjectSO)
    {
        _spawnTimerMax = spawnTimerMax;
        _tablewareSpawnAmountMax = tablewareSpawnAmountMax;
        _tablewareKitchenObjectSO = tablewareKitchenObjectSO;
    }

    public void Update(TablewareCounterModel model)
    {
        if (model.TablewareSpawnAmount < _tablewareSpawnAmountMax)
        {
            model.SpawnTimer += Time.deltaTime;
            if (model.SpawnTimer >= _spawnTimerMax)
            {
                model.SpawnTimer = 0f;
                model.TablewareSpawnAmount++;
                OnTablewareSpawned?.Invoke();
            }
        }
    }

    public void Interact(TablewareCounterModel model, IKitchenObjectParent player)
    {
        if (!player.HasKitchenObject())
        {
            if (model.TablewareSpawnAmount > 0)
            {
                model.TablewareSpawnAmount--;
                KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, player);
                OnTablewareRemoved?.Invoke();
            }
        }
    }
}