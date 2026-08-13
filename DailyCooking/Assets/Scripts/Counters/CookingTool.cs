using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CookingTool : NetworkBehaviour, IHasProgress, IKitchenObjectParent, IHasOptionalSO
{
    public enum State
    {
        Idle,
        Cooking,
        Cooked,
        Burned,
    }

    [SerializeField] private Transform placePoint;
    [SerializeField] private ProgressBarUI progressBarUI;
    [SerializeField] private BurnWarningUI burnWarningUI;
    [SerializeField] private float burnShowProgressAmount = 0.5f;
    [SerializeField] private CombineDetailUI combineDetailUI;
    [SerializeField] private CookingToolConfigSO cookingToolConfig;

    private State _state;
    private float _cookingTimer;
    private float _burningTimer;
    private float _cookingTimeMax;
    private float _burningTimeMax;
    private KitchenObject _kitchenObject;
    private AudioSource cookingSound;
    private AudioSource warningSound;

    private FryingRecipeSO _fryingRecipeSO;
    private BakingRecipeSO _bakingRecipeSO;
    private DeepFryRecipeSO _deepFryRecipeSO;
    private DrinkRecipeSO _drinkRecipeSO;
    private CombineRecipeSO _combineRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public State CurrentState { get => _state; set => _state = value; }
    public float CookingTimer { get => _cookingTimer; set => _cookingTimer = value; }
    public float BurningTimer { get => _burningTimer; set => _burningTimer = value; }
    public float CookingTimeMax { get => _cookingTimeMax; set => _cookingTimeMax = value; }
    public float BurningTimeMax { get => _burningTimeMax; set => _burningTimeMax = value; }

    private CookingToolConfigSO Config
    {
        get
        {
            if (cookingToolConfig != null)
                return cookingToolConfig;

            PlacedObjectView placedObjectView = GetComponentInParent<PlacedObjectView>();
            return placedObjectView != null && placedObjectView.PlacedObjectTypeSO != null
                ? placedObjectView.PlacedObjectTypeSO.cookingToolConfigSO
                : null;
        }
    }

    private FryingRecipeSO[] FryingRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetFryingRecipes() ?? Array.Empty<FryingRecipeSO>();
    private BakingRecipeSO[] BakingRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBakingRecipes() ?? Array.Empty<BakingRecipeSO>();
    private DeepFryRecipeSO[] DeepFryRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetDeepFryRecipes() ?? Array.Empty<DeepFryRecipeSO>();
    private DrinkRecipeSO[] DrinkRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetDrinkRecipes() ?? Array.Empty<DrinkRecipeSO>();
    private CombineRecipeSO[] CombineRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetCombineRecipes() ?? Array.Empty<CombineRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? Array.Empty<BurningRecipeSO>();

    public bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        switch (Config?.toolType ?? CookingToolConfigSO.CookingToolType.Frying)
        {
            case CookingToolConfigSO.CookingToolType.Frying:
                return GetFryingRecipeSOWithInput(inputKitchenObjectSO) != null;
            case CookingToolConfigSO.CookingToolType.Baking:
                return GetBakingRecipeSOWithInput(inputKitchenObjectSO) != null;
            case CookingToolConfigSO.CookingToolType.DeepFry:
                return GetDeepFryRecipeSOWithInput(inputKitchenObjectSO) != null;
            case CookingToolConfigSO.CookingToolType.Beverage:
                return GetDrinkRecipeSOWithInput(inputKitchenObjectSO) != null;
            case CookingToolConfigSO.CookingToolType.Combine:
                return GetCombineRecipeSOWithInput(inputKitchenObjectSO) != null;
            default:
                return false;
        }
    }

    public KitchenObjectSO GetCookingOutput()
    {
        switch (Config?.toolType ?? CookingToolConfigSO.CookingToolType.Frying)
        {
            case CookingToolConfigSO.CookingToolType.Frying:
                return _fryingRecipeSO != null ? _fryingRecipeSO.output : null;
            case CookingToolConfigSO.CookingToolType.Baking:
                return _bakingRecipeSO != null ? _bakingRecipeSO.output : null;
            case CookingToolConfigSO.CookingToolType.DeepFry:
                return _deepFryRecipeSO != null ? _deepFryRecipeSO.output : null;
            case CookingToolConfigSO.CookingToolType.Beverage:
                return _drinkRecipeSO != null ? _drinkRecipeSO.output : null;
            case CookingToolConfigSO.CookingToolType.Combine:
                return _combineRecipeSO != null ? _combineRecipeSO.output : null;
            default:
                return null;
        }
    }

    public KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO != null ? _burningRecipeSO.output : null;
    }

    public void SetCookingRecipeSO()
    {
        KitchenObjectSO input = GetKitchenObject() != null ? GetKitchenObject().GetKitchenObjectSO() : null;
        switch (Config?.toolType ?? CookingToolConfigSO.CookingToolType.Frying)
        {
            case CookingToolConfigSO.CookingToolType.Frying:
                _fryingRecipeSO = GetFryingRecipeSOWithInput(input);
                CookingTimeMax = _fryingRecipeSO != null ? _fryingRecipeSO.fryingTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Baking:
                _bakingRecipeSO = GetBakingRecipeSOWithInput(input);
                CookingTimeMax = _bakingRecipeSO != null ? _bakingRecipeSO.bakingTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.DeepFry:
                _deepFryRecipeSO = GetDeepFryRecipeSOWithInput(input);
                CookingTimeMax = _deepFryRecipeSO != null ? _deepFryRecipeSO.deepFryTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Beverage:
                _drinkRecipeSO = GetDrinkRecipeSOWithInput(input);
                CookingTimeMax = _drinkRecipeSO != null ? _drinkRecipeSO.drinkTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Combine:
                // combine recipe is chosen via option menu (SetOptionKitchenObjectSO)
                break;
        }
    }

    public void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO != null ? _burningRecipeSO.burningTimerMax : 0f;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (FryingRecipeSO fryingRecipeSO in FryingRecipes)
        {
            if (fryingRecipeSO.input == inputKitchenObjectSO)
            {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    private BakingRecipeSO GetBakingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BakingRecipeSO bakingRecipeSO in BakingRecipes)
        {
            if (bakingRecipeSO.input == inputKitchenObjectSO)
            {
                return bakingRecipeSO;
            }
        }
        return null;
    }

    private DeepFryRecipeSO GetDeepFryRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (DeepFryRecipeSO deepFryRecipeSO in DeepFryRecipes)
        {
            if (deepFryRecipeSO.input == inputKitchenObjectSO)
            {
                return deepFryRecipeSO;
            }
        }
        return null;
    }

    private DrinkRecipeSO GetDrinkRecipeSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (DrinkRecipeSO drinkRecipe in DrinkRecipes)
        {
            if (drinkRecipe.input.Contains(kitchenObjectSO))
                return drinkRecipe;
        }
        return null;
    }

    private CombineRecipeSO GetCombineRecipeSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (CombineRecipeSO combineRecipe in CombineRecipes)
        {
            if (combineRecipe.input.Contains(kitchenObjectSO))
                return combineRecipe;
        }
        return null;
    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipeSO in BurningRecipes)
        {
            if (burningRecipeSO.input == inputKitchenObjectSO)
            {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public void UpdateCookingState(State state)
    {
        CurrentState = state;

        CookingTimer = 0;

        BurningTimer = 0;

        FireOnStateChange();
    }

    public void FireOnStateChange()
    {
        if (CurrentState == State.Idle || CurrentState == State.Burned)
        {
            SoundManager.Instance.StopSound(cookingSound);
            SoundManager.Instance.StopSound(warningSound);
        }
        if (CurrentState == State.Cooking || CurrentState == State.Cooked)
        {
            if (cookingSound == null || !cookingSound.isPlaying)
                cookingSound = SoundManager.Instance.PlayCookingSound(gameObject.transform.position);
        }
    }

    private void Update()
    {
        if (HasKitchenObject())
        {
            switch (CurrentState)
            {
                case CookingTool.State.Idle:
                    break;
                case CookingTool.State.Cooking:
                    CookingTimer += Time.deltaTime;

                    progressBarUI.OnProgressChanged(CookingTimer / CookingTimeMax);
                    if (CookingTimer > CookingTimeMax)
                    {
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetCookingOutput(), this);
                        CurrentState = CookingTool.State.Cooked;
                        BurningTimer = 0f;
                        SetBurningRecipeSO(GetKitchenObject().GetKitchenObjectSO());

                        FireOnStateChange();
                    }
                    break;

                case CookingTool.State.Cooked:
                    BurningTimer += Time.deltaTime;

                    progressBarUI.OnProgressChanged(BurningTimer / BurningTimeMax);
                    burnWarningUI.OnProgressChanged(this, BurningTimer / BurningTimeMax);

                    if (GetProgress() >= burnShowProgressAmount && (warningSound == null || !warningSound.isPlaying))
                    {
                        warningSound = SoundManager.Instance.PlayWarningSound(gameObject.transform.position);
                    }

                    if (BurningTimer > BurningTimeMax)
                    {
                        GetKitchenObject().DestroySelf();
                        KitchenObject.SpawnKitchenObject(GetBurningOutput(), this);

                        CurrentState = CookingTool.State.Burned;

                        FireOnStateChange();
                    }
                    break;
                case CookingTool.State.Burned:
                    progressBarUI.Hide();
                    burnWarningUI.Hide();
                    break;
            }
        }
    }

    public virtual void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        _kitchenObject = kitchenObject;
        progressBarUI.Hide();
        burnWarningUI.Hide();

        if (Config != null && Config.supportsOptionMenu && kitchenObject != null)
        {
            List<KitchenObjectSO> options = GetListKitchenObjectList(kitchenObject.GetKitchenObjectSO());
            if (options != null && options.Count > 0)
            {
                UIPopupManager.Instance.ShowPopup(
                    UIPopupType.UIOptionMenuPopup,
                    new UIOptionMenuPopup.Param
                    {
                        sender = this,
                        optionalList = options,
                        Title = "Select way to process ingredient:"
                    }
                );
            }
        }
    }

    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        return placePoint;
    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        return _kitchenObject;
    }

    public void ClearKitchenObject(int index = 0)
    {
        _kitchenObject = null;
        progressBarUI.Hide();
        burnWarningUI.Hide();
    }

    public bool HasKitchenObject(int index = 0)
    {
        return _kitchenObject != null;
    }

    public bool IsDone()
    {
        return CurrentState == CookingTool.State.Cooked;
    }

    public float GetProgress()
    {
        if (CurrentState == CookingTool.State.Cooking)
        {
            return CookingTimer / CookingTimeMax;
        }
        else if (CurrentState == CookingTool.State.Cooked)
        {
            return BurningTimer / BurningTimeMax;
        }
        else
        {
            return 0;
        }
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (Config?.toolType != CookingToolConfigSO.CookingToolType.Combine)
            return;

        KitchenObjectSO input = GetKitchenObject() != null ? GetKitchenObject().GetKitchenObjectSO() : null;
        if (input == null)
            return;

        List<KitchenObjectSO> options = GetListKitchenObjectList(input);
        if (index < 0 || index >= options.Count)
            return;

        KitchenObjectSO chosenOutput = options[index];
        _combineRecipeSO = null;
        foreach (CombineRecipeSO combineRecipe in CombineRecipes)
        {
            if (combineRecipe.input.Contains(input) && combineRecipe.output == chosenOutput)
            {
                _combineRecipeSO = combineRecipe;
                break;
            }
        }

        CookingTimeMax = _combineRecipeSO != null ? _combineRecipeSO.combineTimerMax : 0f;

        if (combineDetailUI != null)
            combineDetailUI.InitUI(_combineRecipeSO);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        List<KitchenObjectSO> kitchenObjectSOs = new List<KitchenObjectSO>();
        if (Config?.toolType != CookingToolConfigSO.CookingToolType.Combine)
            return kitchenObjectSOs;

        foreach (CombineRecipeSO combineRecipe in CombineRecipes)
        {
            if (combineRecipe.input.Contains(kitchenObjectSO))
                kitchenObjectSOs.Add(combineRecipe.output);
        }
        return kitchenObjectSOs;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}
