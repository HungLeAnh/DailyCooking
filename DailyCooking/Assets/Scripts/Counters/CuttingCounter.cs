using System;
using UnityEngine;


public class CuttingCounter : BaseCounter, IHasProgress
{

    public static event EventHandler OnAnyCut;

    new public static void ResetStaticData()
    {
        OnAnyCut = null;
    }

    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    public event EventHandler OnCut;

    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;

    private int cuttingProgress = 0;
    private CuttingRecipeSO _cuttingRecipeSO = null;
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

                    if (GetKitchenObject().GetKitchenObjectOptionalProcessSO()!=null)
                    {
                        //Show optional recipe menu
                        _cuttingRecipeSO = null;
                    }
                    else
                    {
                        _cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

                        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                        {
                            progressNormalized = (float)cuttingProgress / _cuttingRecipeSO.cuttingProgressMax
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
            if(cuttingProgress == 0)
            {
                //There is kitchen object here not process / done process
                if (playerStateMachine.HasKitchenObject())
                {
                    //Player is carrying something
                    if (playerStateMachine.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
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
            cuttingProgress++;

            OnCut?.Invoke(this, EventArgs.Empty);
            OnAnyCut?.Invoke(this, EventArgs.Empty);

            if(_cuttingRecipeSO == null)
                _cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
            {
                progressNormalized = (float)cuttingProgress / _cuttingRecipeSO.cuttingProgressMax
            });

            if (cuttingProgress >= _cuttingRecipeSO.cuttingProgressMax)
            {

                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(_cuttingRecipeSO.output, this);
                cuttingProgress = 0;
                _cuttingRecipeSO = null;
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
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.input == inputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }    
    private CuttingRecipeSO GetCuttingRecipeSOWithOutput(KitchenObjectSO outputKitchenObjectSO)
    {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.output == outputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    public override void SetOptionKitchenObjectSO(KitchenObjectSO kitchenObjectSO)
    {
        _cuttingRecipeSO = GetCuttingRecipeSOWithOutput(kitchenObjectSO);

        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = (float)cuttingProgress / _cuttingRecipeSO.cuttingProgressMax
        });
    }
}
