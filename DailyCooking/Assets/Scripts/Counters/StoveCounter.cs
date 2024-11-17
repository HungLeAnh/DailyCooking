using System;
using UnityEngine;

public class StoveCounter : BaseCounter, IHasProgress
{
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    [SerializeField] private CookingTool _cookingTool;

    private void Start()
    {
        _cookingTool.CurrentState = CookingTool.State.Idle;
    }

    private void Update()
    {
        if (HasKitchenObject())
        {
            switch (_cookingTool.CurrentState)
            {
                case CookingTool.State.Idle:
                    break;
                case CookingTool.State.Cooking:
                    _cookingTool.CookingTimer += Time.deltaTime;

                    OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                    {
                        progressNormalized = _cookingTool.CookingTimer / _cookingTool.CookingTimeMax
                    });

                    if (_cookingTool.CookingTimer > _cookingTool.CookingTimeMax)
                    {
                        //Fried
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(_cookingTool.GetCookingOutput(), this);
                        _cookingTool.CurrentState = CookingTool.State.Cooked;
                        _cookingTool.BurningTimer = 0f;
                        _cookingTool.SetBurningRecipeSO(GetKitchenObject().GetKitchenObjectSO());

                        _cookingTool.FireOnStateChange();

                    }
                    break;

                case CookingTool.State.Cooked:
                    _cookingTool.BurningTimer += Time.deltaTime;

                    OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                    {
                        progressNormalized = _cookingTool.BurningTimer / _cookingTool.BurningTimeMax
                    });

                    if (_cookingTool.BurningTimer > _cookingTool.BurningTimeMax)
                    {
                        //Fried
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(_cookingTool.GetBurningOutput(), this);

                        _cookingTool.CurrentState = CookingTool.State.Burned;

                        _cookingTool.FireOnStateChange();

                        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                        {
                            progressNormalized = 0f
                        });
                    }
                    break;
                case CookingTool.State.Burned:
                    break;
            }

        }
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (!HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                KitchenObjectSO kitchenObjectSO = playerStateMachine.GetKitchenObject().GetKitchenObjectSO();
                //Player is carrying something
                if (_cookingTool.HasRecipeWithInput(kitchenObjectSO))
                {
                    //Player is carrying something that can be fried
                    //IHasOptionalSO option = (IHasOptionalSO)_cookingTool;
                    //if (option != null)
                    //{
                    //    FireOnShowCombineRecipe(option.GetListKitchenObjectList(kitchenObjectSO));
                    //}

                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);
                    
                    _cookingTool.SetCookingRecipeSO(GetKitchenObject().GetKitchenObjectSO());

                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking,0f);

                    OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                    {
                        progressNormalized = _cookingTool.CookingTimer / _cookingTool.CookingTimeMax
                    });
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
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        GetKitchenObject().DestroySelf();
                        
                        _cookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);

                        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                        {
                            progressNormalized = 0f
                        });
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(playerStateMachine);

                _cookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);

                OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
                {
                    progressNormalized = 0f
                });
            }
        }
    }
    
    public bool IsFried()
    {
        return _cookingTool.CurrentState == CookingTool.State.Cooked;
    }
}
