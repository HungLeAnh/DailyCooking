using Observer;
using System;
[Serializable]
public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    public static event EventHandler OnAnyCut;

    new public static void ResetStaticData()
    {
        OnAnyCut = null;
    }

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnCut;

    private CuttingCounterModel _model;
    private CuttingCounterView _view;

    public CuttingCounterController(CuttingCounterView view, CuttingCounterModel model) : base(view,model)
    {
        _model = model;
        _view = view;
    }
    
    public override void Interact(PlayerStateMachine playerStateMachine)
    {
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
                        _model.CuttingRecipeSO = null;
                        _model.NotifySubscribers(EObserverEvent.ModelChange);
                    }
                    else
                    {
                        _model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                        _model.NotifySubscribers(EObserverEvent.ModelChange);

                        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                        {
                            progressNormalized = (float)_model.CuttingProgress / _model.CuttingRecipeSO.cuttingProgressMax
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
            if (_model.CuttingProgress == 0)
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
    public override void InteractAlternate(PlayerStateMachine playerStateMachine)
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            //there is kitchen object here
            _model.CuttingProgress++;

            OnCut?.Invoke(this, EventArgs.Empty);
            OnAnyCut?.Invoke(this, EventArgs.Empty);

            if (_model.CuttingRecipeSO == null)
                _model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
            {
                progressNormalized = (float)_model.CuttingProgress / _model.CuttingRecipeSO.cuttingProgressMax
            });

            _model.NotifySubscribers(EObserverEvent.ModelChange);

            if (_model.CuttingProgress >= _model.CuttingRecipeSO.cuttingProgressMax)
            {

                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(_model.CuttingRecipeSO.output, this);
                _model.CuttingProgress = 0;
                _model.CuttingRecipeSO = null;
                _model.NotifySubscribers(EObserverEvent.ModelChange);
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
        foreach (CuttingRecipeSO cuttingRecipeSO in _view.CuttingRecipeSOArray)
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
        var outputKitchenObjectSO = GetKitchenObject().GetKitchenObjectOptionalProcessSO().processListOutput[outputKitchenObjectSOIndex];
        foreach (CuttingRecipeSO cuttingRecipeSO in _view.CuttingRecipeSOArray)
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

        _model.CuttingRecipeSO = GetCuttingRecipeSOWithOutput(index);

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = (float)_model.CuttingProgress / _model.CuttingRecipeSO.cuttingProgressMax
        });
        _model.NotifySubscribers(EObserverEvent.ModelChange);
    }
}