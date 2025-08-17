using System;
using UnityEngine;

public class StoveCounterController : BaseCounterController
{
    [SerializeField] private CookingTool _cookingTool;
    private StoveCounterModel _stoveCounterModel;
    private StoveCounterService _stoveCounterService;

    private void Awake()
    {
        _stoveCounterModel = new StoveCounterModel();
        BaseCounterModel = _stoveCounterModel;
        _stoveCounterService = new StoveCounterService(_cookingTool);
    }

    protected override void OnRestartGame(object sender, PlayerStateMachine e)
    {
        base.OnRestartGame(sender, e);

        if (_cookingTool.HasKitchenObject())
            _cookingTool.GetKitchenObject().DestroySelf();

        _cookingTool.ClearKitchenObject();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _stoveCounterService.Interact(this, playerStateMachine);
    }

    public float GetProgress()
    {
        return _cookingTool.GetProgress();
    }

    public bool IsDone()
    {
        return _cookingTool.IsDone();
    }
}
