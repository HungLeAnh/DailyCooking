using Observer;
using System;
[Serializable]
public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnCut;



    public CuttingCounterController(CuttingCounterView view, CuttingCounterModel model) : base(view,model)
    {

    }
    protected override void BaseCounterView_OnRestartGame(object sender, PlayerStateMachine e)
    {
        base.BaseCounterView_OnRestartGame(sender, e);

        var model = (CuttingCounterModel)BaseCounterModel;
        model.CuttingRecipeSO = null;
        model.CuttingProgress = 0;
        model.NotifySubscribers(EObserverEvent.ModelChange);

    }
    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        var model = (CuttingCounterModel)BaseCounterModel;
        if (!HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (HasRecipeWithInput(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                {
                    //Player is carrying something that can be cut
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);

                    if (GetKitchenObject().GetKitchenObjectOptionalProcessSO() != null)
                    {
                        //Show optional recipe menu
                        model.CuttingRecipeSO = null;
                        model.NotifySubscribers(EObserverEvent.ModelChange);
                    }
                    else
                    {
                        model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                        model.NotifySubscribers(EObserverEvent.ModelChange);

                        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                        {
                            progressNormalized = (float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax
                        });
                    }
                }
            }
            else
            {
                //Player is not carrying anything
            }
        }
        else
        {
            if (model.CuttingProgress == 0)
            {
                //There is kitchen object here not process / done process
                if (playerStateMachine.HasKitchenObject())
                {
                    //Player is carrying something
                    if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                    {
                        //Player is holding a plate
                        if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                        {
                            GetKitchenObject().DestroySelf();
                        }
                    }
                }
                else
                {
                    //Player is not carrying anything
                    GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
                }
            }
        }
    }
    public override void ProcessKitchenObject(PlayerStateMachine playerStateMachine)
    {
        var model = (CuttingCounterModel)BaseCounterModel;
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            //there is kitchen object here
            model.CuttingProgress++;

            OnCut?.Invoke(this, EventArgs.Empty);

            if (model.CuttingRecipeSO == null)
                model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
            {
                progressNormalized = (float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax
            });

            model.NotifySubscribers(EObserverEvent.ModelChange);

            if (model.CuttingProgress >= model.CuttingRecipeSO.cuttingProgressMax)
            {

                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(model.CuttingRecipeSO.output, this);
                model.CuttingProgress = 0;
                model.CuttingRecipeSO = null;
                model.NotifySubscribers(EObserverEvent.ModelChange);
            }
        }
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        return cuttingRecipeSO != null;

    }
    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        if (cuttingRecipeSO != null)
        {
            return cuttingRecipeSO.output;
        }
        else
        {
            return null;
        }
    }
    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        var view = (CuttingCounterView)BaseCounterView;
        foreach (CuttingRecipeSO cuttingRecipeSO in view.CuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.input == inputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }
    private CuttingRecipeSO GetCuttingRecipeSOWithOutput(int outputKitchenObjectSOIndex)
    {
        var view = (CuttingCounterView)BaseCounterView;

        var outputKitchenObjectSO = GetKitchenObject().GetKitchenObjectOptionalProcessSO().processListOutput[outputKitchenObjectSOIndex];
        foreach (CuttingRecipeSO cuttingRecipeSO in view.CuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.output == outputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        var model = (CuttingCounterModel)BaseCounterModel;

        model.CuttingRecipeSO = GetCuttingRecipeSOWithOutput(index);

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = (float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax
        });
        model.NotifySubscribers(EObserverEvent.ModelChange);
    }

    public bool IsDone()
    {
        var model = (CuttingCounterModel)BaseCounterModel;
        if(model.CuttingProgress == 0 && 
            model.CuttingRecipeSO == null &&
            model.KitchenObject != null)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public int GetProgress()
    {
        var model = (CuttingCounterModel)BaseCounterModel;
        return model.CuttingProgress;
    }
}