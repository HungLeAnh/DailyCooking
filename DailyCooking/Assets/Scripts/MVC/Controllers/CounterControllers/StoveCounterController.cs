using Observer;
using System;
using UnityEngine;

public class StoveCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new StoveCounterModel();
        Init();
    }
    
    protected override void OnRestartGame(object sender, PlayerStateMachine e)
    {
        base.OnRestartGame(sender, e);

        var view = (StoveCounterView)BaseCounterView;

        if (view.CookingTool.HasKitchenObject())
            view.CookingTool.GetKitchenObject().DestroySelf();

        view.CookingTool.ClearKitchenObject();

    }
    private void Init()
    {
        var view = (StoveCounterView)BaseCounterView;
        view.CookingTool.CurrentState = CookingTool.State.Idle;
        view.CookingTool.OnStageChanged += CookingTool_OnStageChanged;

        var model = (StoveCounterModel)BaseCounterModel;
        model.AudioSource = view.AudioSource;
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        var view = (StoveCounterView)BaseCounterView;

        if (!view.CookingTool.HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                KitchenObjectSO kitchenObjectSO = playerStateMachine.GetKitchenObject().GetKitchenObjectSO();
                //Player is carrying something
                if (view.CookingTool.HasRecipeWithInput(kitchenObjectSO))
                {
                    //Player is carrying something that can be fried
                    //IHasOptionalSO option = (IHasOptionalSO)_cookingTool;
                    //if (option != null)
                    //{
                    //    FireOnShowCombineRecipe(option.GetListKitchenObjectList(kitchenObjectSO));
                    //}

                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(view.CookingTool);

                    view.CookingTool.SetCookingRecipeSO();

                    view.CookingTool.UpdateCookingState(CookingTool.State.Cooking);

                }
            }
            else
            {
                //Player is not carrying anything
            }
        }
        else
        {
            //There is kitchen object here
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                {
                    //Player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(view.CookingTool.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        view.CookingTool.GetKitchenObject().DestroySelf();

                        view.CookingTool.UpdateCookingState(CookingTool.State.Idle);
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                view.CookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);

                view.CookingTool.UpdateCookingState(CookingTool.State.Idle);

            }
        }
    }
    private void CookingTool_OnStageChanged(object sender, CookingTool.OnStageChangeEventArgs e)
    {
        var model = (StoveCounterModel)BaseCounterModel;
        bool playSound = e.state == CookingTool.State.Cooking || e.state == CookingTool.State.Cooked;
        if (playSound)
        {
            model.AudioSource.Play();
        }
        else
        {
            model.AudioSource.Pause();
        }
    }
    public void PlayWarningSound()
    {
        var model = (StoveCounterModel)BaseCounterModel;
        var view = (StoveCounterView)BaseCounterView;

        float burnShowProgressAmount = .5f;

        model.PlayWarningSound = view.CookingTool.IsDone() && view.CookingTool.GetProgress() >= burnShowProgressAmount;

    }
}