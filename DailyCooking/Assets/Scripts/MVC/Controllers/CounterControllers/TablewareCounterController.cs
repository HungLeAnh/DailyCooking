using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
public class TablewareCounterController : BaseCounterController
{
    private void Update()
    {
        var view = (TablewareCounterView)BaseCounterView;
        var model = (TablewareCounterModel)BaseCounterModel;
        if (model.TablewareSpawnAmount < view.TablewareSpawnAmountMax)
        {
            model.SpawnTimer += Time.deltaTime;
            if (model.SpawnTimer >= view.SpawnTimerMax)
            {
                model.SpawnTimer = 0f;
                model.TablewareSpawnAmount++;
                view.FireOnTablewareSpawned();
            }
        }
    }
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        var view = (TablewareCounterView)BaseCounterView;
        var model = (TablewareCounterModel)BaseCounterModel;
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is empty handed
            if (model.TablewareSpawnAmount > 0)
            {
                //There is at least one tableware
                model.TablewareSpawnAmount--;
                KitchenObject.SpawnKitchenObject(view.TablewareKitchenObjectSO, playerStateMachine);
                view.FireOnTablewareRemoved();
            }
        }
    }
}