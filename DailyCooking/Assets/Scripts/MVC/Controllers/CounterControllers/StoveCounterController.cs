
using Observer;
using System;
using UnityEngine;

public class StoveCounterController : BaseCounterController
{
    [SerializeField] private CookingTool _cookingTool;
    [SerializeField] private AudioSource _audioSource;
    private StoveCounterModel _stoveCounterModel;

    protected override void Awake()
    {
        base.Awake();
        _stoveCounterModel = new StoveCounterModel();
        BaseCounterModel = _stoveCounterModel;
        Init();
    }

    protected override void OnRestartGame(object sender, PlayerStateMachine e)
    {
        base.OnRestartGame(sender, e);

        if (_cookingTool.HasKitchenObject())
            _cookingTool.GetKitchenObject().DestroySelf();

        _cookingTool.ClearKitchenObject();
    }

    private void Init()
    {
        _cookingTool.CurrentState = CookingTool.State.Idle;
        _cookingTool.OnStageChanged += CookingTool_OnStageChanged;
        
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!_cookingTool.HasKitchenObject())
        {
            //There is no kitchen object in cooking tool
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (_cookingTool.HasRecipeWithInput(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                {
                    //Player is carrying something that can be fried
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(_cookingTool);
                    _cookingTool.SetCookingRecipeSO();
                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
                }
            }
        }
        else
        {
            //There is kitchen object in cooking tool
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                HandlePlateInteraction(playerStateMachine);
            }
            else
            {
                //Player is not carrying anything
                _cookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
                _cookingTool.UpdateCookingState(CookingTool.State.Idle);
            }
        }
    }

    private void HandlePlateInteraction(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
        {
            //Player is holding a plate
            if (plateKitchenObject.TryAddIngredient(_cookingTool.GetKitchenObject().GetKitchenObjectSO()))
            {
                _cookingTool.GetKitchenObject().DestroySelf();
                _cookingTool.UpdateCookingState(CookingTool.State.Idle);
            }
        }
    }

    private void CookingTool_OnStageChanged(object sender, CookingTool.OnStageChangeEventArgs e)
    {
        bool playSound = e.state == CookingTool.State.Cooking || e.state == CookingTool.State.Cooked;
        if (playSound)
        {
            _audioSource.Play();
        }
        else
        {
            _audioSource.Pause();
        }
    }

    public void PlayWarningSound()
    {
        float burnShowProgressAmount = .5f;
        _stoveCounterModel.PlayWarningSound = _cookingTool.IsDone() && _cookingTool.GetProgress() >= burnShowProgressAmount;
    }
}
