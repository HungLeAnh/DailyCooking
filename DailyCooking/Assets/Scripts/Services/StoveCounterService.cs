using System;
using UnityEngine;

public class StoveCounterService
{
    public enum State
    {
        Idle,
        Cooking,
        Cooked,
        Burned,
    }

    public event EventHandler<State> OnStateChanged;
    public event EventHandler<float> OnProgressChanged;

    private readonly CookingRecipeSO[] _cookingRecipeSOArray;
    private readonly BurningRecipeSO[] _burningRecipeSOArray;

    public StoveCounterService(CookingRecipeSO[] cookingRecipeSOArray, BurningRecipeSO[] burningRecipeSOArray)
    {
        _cookingRecipeSOArray = cookingRecipeSOArray;
        _burningRecipeSOArray = burningRecipeSOArray;
    }

    public void Interact(StoveCounterModel model, IKitchenObjectParent counter, IKitchenObjectParent player)
    {
        if (!counter.HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                if (HasCookingRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    player.GetKitchenObject().SetKitchenObjectParent(counter);
                    model.CurrentState = State.Cooking;
                    model.CookingTimer = 0;
                    OnStateChanged?.Invoke(this, model.CurrentState);
                }
            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
                {
                    if (tablewareKitchenObject.TryAddIngredient(counter.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        counter.GetKitchenObject().DestroySelf();
                        model.CurrentState = State.Idle;
                        OnStateChanged?.Invoke(this, model.CurrentState);
                        OnProgressChanged?.Invoke(this, 0f);
                    }
                }
            }
            else
            {
                counter.GetKitchenObject().SetKitchenObjectParent(player);
                model.CurrentState = State.Idle;
                OnStateChanged?.Invoke(this, model.CurrentState);
                OnProgressChanged?.Invoke(this, 0f);
            }
        }
    }

    public void Update(StoveCounterModel model, IKitchenObjectParent counter)
    {
        if (counter.HasKitchenObject())
        {
            switch (model.CurrentState)
            {
                case State.Idle:
                    break;
                case State.Cooking:
                    model.CookingTimer += Time.deltaTime;
                    OnProgressChanged?.Invoke(this, model.CookingTimer / GetCookingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).cookingTimerMax);
                    if (model.CookingTimer > GetCookingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).cookingTimerMax)
                    {
                        counter.GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetCookingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).output, counter);
                        model.CurrentState = State.Cooked;
                        model.BurningTimer = 0f;
                        OnStateChanged?.Invoke(this, model.CurrentState);
                    }
                    break;
                case State.Cooked:
                    model.BurningTimer += Time.deltaTime;
                    OnProgressChanged?.Invoke(this, model.BurningTimer / GetBurningRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).burningTimerMax);
                    if (model.BurningTimer > GetBurningRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).burningTimerMax)
                    {
                        counter.GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetBurningRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO()).output, counter);
                        model.CurrentState = State.Burned;
                        OnStateChanged?.Invoke(this, model.CurrentState);
                        OnProgressChanged?.Invoke(this, 0f);
                    }
                    break;
                case State.Burned:
                    OnProgressChanged?.Invoke(this, 0f);
                    break;
            }
        }
    }

    private bool HasCookingRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetCookingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private CookingRecipeSO GetCookingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (CookingRecipeSO cookingRecipeSO in _cookingRecipeSOArray)
        {
            if (cookingRecipeSO.input == inputKitchenObjectSO)
            {
                return cookingRecipeSO;
            }
        }
        return null;
    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipeSO in _burningRecipeSOArray)
        {
            if (burningRecipeSO.input == inputKitchenObjectSO)
            {
                return burningRecipeSO;
            }
        }
        return null;
    }
}
