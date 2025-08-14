using Observer;
using System;
using UnityEngine;

public class StoveCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
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
        view.CookingTool.OnProgressChanged += StoveCounter_OnProgressChanged;

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

                    view.CookingTool.UpdateCookingState(CookingTool.State.Cooking, 0f);
                    view.CookingTool.FireOnProgressChanged(view.CookingTool.CookingTimer / view.CookingTool.CookingTimeMax);
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

                        view.CookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);
                        view.CookingTool.FireOnProgressChanged(0f);
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                view.CookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);

                view.CookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);

                view.CookingTool.FireOnProgressChanged(0f);

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

    private void StoveCounter_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        var model = (StoveCounterModel)BaseCounterModel;
        var view = (StoveCounterView)BaseCounterView;

        float burnShowProgressAmount = .5f;

        model.PlayWarningSound = view.CookingTool.IsDone() && e.progressNormalized >= burnShowProgressAmount;

    }

    
}