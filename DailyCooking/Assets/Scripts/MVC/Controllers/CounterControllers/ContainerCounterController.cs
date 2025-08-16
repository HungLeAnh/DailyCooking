using System;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    [SerializeField] private KitchenObjectSO _kitchenObjectSO;

    private ContainerCounterService _containerCounterService;

    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
        _containerCounterService = new ContainerCounterService();

        _containerCounterService.OnSpawnKitchenObject += (sender, spawnedKitchenObjectSO) =>
        {

        };
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _containerCounterService.Interact(playerStateMachine, _kitchenObjectSO);
    }

    public KitchenObjectSO GetContainerKitchenObjectType()
    {
        return _kitchenObjectSO;
    }
}
