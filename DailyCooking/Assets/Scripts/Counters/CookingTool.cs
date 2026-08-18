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

    private readonly NetworkVariable<State> _netState = new NetworkVariable<State>(State.Idle);
    private readonly NetworkVariable<float> _netCookingTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<float> _netBurningTimer = new NetworkVariable<float>(0f);
    private readonly NetworkVariable<int> _netCombineRecipeIndex = new NetworkVariable<int>(-1);

    private KitchenObject _kitchenObject;
    private AudioSource cookingSound;
    private AudioSource warningSound;

    private CookingToolConfigSO _cachedConfig;
    private bool _configResolved;
    private RecipeDatabaseSO _recipeDatabase;

    private float _cookingTimeMax;
    private float _burningTimeMax;
    private bool _recipeResolved;
    private bool _burningResolved;

    private FryingRecipeSO _fryingRecipeSO;
    private BakingRecipeSO _bakingRecipeSO;
    private DeepFryRecipeSO _deepFryRecipeSO;
    private DrinkRecipeSO _drinkRecipeSO;
    private CombineRecipeSO _combineRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public State CurrentState => _netState.Value;
    public float CookingTimer => _netCookingTimer.Value;
    public float BurningTimer => _netBurningTimer.Value;
    public float CookingTimeMax => _cookingTimeMax;
    public float BurningTimeMax => _burningTimeMax;

    private CookingToolConfigSO Config
    {
        get
        {
            if (!_configResolved)
            {
                _configResolved = true;
                _cachedConfig = cookingToolConfig;
                if (_cachedConfig == null)
                {
                    PlacedObjectView placedObjectView = GetComponentInParent<PlacedObjectView>();
                    _cachedConfig = placedObjectView != null && placedObjectView.PlacedObjectTypeSO != null
                        ? placedObjectView.PlacedObjectTypeSO.cookingToolConfigSO
                        : null;
                }
            }
            return _cachedConfig;
        }
    }

    private RecipeDatabaseSO RecipeDatabase
    {
        get
        {
            if (_recipeDatabase == null && KitchenGameManager.Instance != null)
                _recipeDatabase = KitchenGameManager.Instance.RecipeDatabase;
            return _recipeDatabase;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _netState.OnValueChanged += HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged += HandleCombineIndexChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _netState.OnValueChanged -= HandleStateChanged;
        _netCombineRecipeIndex.OnValueChanged -= HandleCombineIndexChanged;
    }

    private void HandleStateChanged(State previousValue, State newValue)
    {
        FireOnStateChange();
    }

    private void HandleCombineIndexChanged(int previousValue, int newValue)
    {
        if (newValue < 0) return;
        KitchenObjectSO input = _kitchenObject != null ? _kitchenObject.GetKitchenObjectSO() : null;
        if (input == null) return;

        List<KitchenObjectSO> options = GetListKitchenObjectList(input);
        if (newValue < 0 || newValue >= options.Count) return;
        ApplyCombineRecipe(input, options[newValue]);
    }

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
        KitchenObjectSO input = _kitchenObject != null ? _kitchenObject.GetKitchenObjectSO() : null;
        switch (Config?.toolType ?? CookingToolConfigSO.CookingToolType.Frying)
        {
            case CookingToolConfigSO.CookingToolType.Frying:
                _fryingRecipeSO = GetFryingRecipeSOWithInput(input);
                _cookingTimeMax = _fryingRecipeSO != null ? _fryingRecipeSO.fryingTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Baking:
                _bakingRecipeSO = GetBakingRecipeSOWithInput(input);
                _cookingTimeMax = _bakingRecipeSO != null ? _bakingRecipeSO.bakingTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.DeepFry:
                _deepFryRecipeSO = GetDeepFryRecipeSOWithInput(input);
                _cookingTimeMax = _deepFryRecipeSO != null ? _deepFryRecipeSO.deepFryTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Beverage:
                _drinkRecipeSO = GetDrinkRecipeSOWithInput(input);
                _cookingTimeMax = _drinkRecipeSO != null ? _drinkRecipeSO.drinkTimerMax : 0f;
                break;
            case CookingToolConfigSO.CookingToolType.Combine:
                // combine recipe is chosen via option menu (SetOptionKitchenObjectSO)
                break;
        }
    }

    public void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        _burningTimeMax = _burningRecipeSO != null ? _burningRecipeSO.burningTimerMax : 0f;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return RecipeDatabase?.GetFryingRecipe(inputKitchenObjectSO);
    }

    private BakingRecipeSO GetBakingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return RecipeDatabase?.GetBakingRecipe(inputKitchenObjectSO);
    }

    private DeepFryRecipeSO GetDeepFryRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return RecipeDatabase?.GetDeepFryRecipe(inputKitchenObjectSO);
    }

    private DrinkRecipeSO GetDrinkRecipeSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        return RecipeDatabase?.GetDrinkRecipeByIngredient(kitchenObjectSO);
    }

    private CombineRecipeSO GetCombineRecipeSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        return RecipeDatabase?.GetCombineRecipe(kitchenObjectSO);
    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return RecipeDatabase?.GetBurningRecipe(inputKitchenObjectSO);
    }

    public void UpdateCookingState(State state)
    {
        UpdateCookingStateServerRpc(state);
    }

    [Rpc(SendTo.Server)]
    private void UpdateCookingStateServerRpc(State state)
    {
        _netCookingTimer.Value = 0f;
        _netBurningTimer.Value = 0f;
        _netState.Value = state;
    }

    public void FireOnStateChange()
    {
        if (_netState.Value == State.Idle || _netState.Value == State.Burned)
        {
            SoundManager.Instance.StopSound(cookingSound);
            SoundManager.Instance.StopSound(warningSound);
        }
        if (_netState.Value == State.Cooking || _netState.Value == State.Cooked)
        {
            if (cookingSound == null || !cookingSound.isPlaying)
                cookingSound = SoundManager.Instance.PlayCookingSound(gameObject.transform.position);
        }
    }

    private void Update()
    {
        if (_kitchenObject == null)
            return;

        if (IsServer)
            AdvanceCooking();

        UpdateUI();
    }

    private void AdvanceCooking()
    {
        State state = _netState.Value;
        switch (state)
        {
            case CookingTool.State.Idle:
                break;
            case CookingTool.State.Cooking:
                if (!_recipeResolved)
                {
                    SetCookingRecipeSO();
                    _recipeResolved = _cookingTimeMax > 0f;
                }
                if (_cookingTimeMax <= 0f)
                    break;

                _netCookingTimer.Value += Time.deltaTime;
                if (_netCookingTimer.Value > _cookingTimeMax)
                {
                    _kitchenObject.DestroySelf();
                    KitchenObject.SpawnKitchenObject(GetCookingOutput(), this);
                    _netState.Value = CookingTool.State.Cooked;
                    _netBurningTimer.Value = 0f;
                    SetBurningRecipeSO(_kitchenObject.GetKitchenObjectSO());
                }
                break;

            case CookingTool.State.Cooked:
                if (_burningRecipeSO == null)
                    break;

                _netBurningTimer.Value += Time.deltaTime;
                if (_netBurningTimer.Value > _burningTimeMax)
                {
                    _kitchenObject.DestroySelf();
                    KitchenObject.SpawnKitchenObject(GetBurningOutput(), this);
                    _netState.Value = CookingTool.State.Burned;
                }
                break;
            case CookingTool.State.Burned:
                break;
        }
    }

    private void UpdateUI()
    {
        State state = _netState.Value;
        switch (state)
        {
            case CookingTool.State.Idle:
                break;
            case CookingTool.State.Cooking:
                if (!_recipeResolved)
                {
                    SetCookingRecipeSO();
                    _recipeResolved = _cookingTimeMax > 0f;
                }
                if (_cookingTimeMax > 0f)
                    progressBarUI.OnProgressChanged(_netCookingTimer.Value / _cookingTimeMax);
                break;

            case CookingTool.State.Cooked:
                if (!_burningResolved && _kitchenObject != null)
                {
                    SetBurningRecipeSO(_kitchenObject.GetKitchenObjectSO());
                    _burningResolved = true;
                }

                if (_burningRecipeSO == null)
                {
                    progressBarUI.OnProgressChanged(1f);
                    burnWarningUI.Hide();
                    break;
                }

                float burnProgress = _netBurningTimer.Value / _burningTimeMax;
                progressBarUI.OnProgressChanged(burnProgress);
                burnWarningUI.OnProgressChanged(this, burnProgress);

                if (burnProgress >= burnShowProgressAmount && (warningSound == null || !warningSound.isPlaying))
                {
                    warningSound = SoundManager.Instance.PlayWarningSound(gameObject.transform.position);
                }
                break;
            case CookingTool.State.Burned:
                progressBarUI.Hide();
                burnWarningUI.Hide();
                break;
        }
    }

    public virtual void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        _kitchenObject = kitchenObject;
        _recipeResolved = false;
        _burningResolved = false;
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
        _recipeResolved = false;
        _burningResolved = false;
        progressBarUI.Hide();
        burnWarningUI.Hide();
    }

    public bool HasKitchenObject(int index = 0)
    {
        return _kitchenObject != null;
    }

    public bool IsDone()
    {
        return _netState.Value == CookingTool.State.Cooked;
    }

    public float GetProgress()
    {
        if (_netState.Value == CookingTool.State.Cooking)
        {
            return _cookingTimeMax > 0f ? _netCookingTimer.Value / _cookingTimeMax : 0f;
        }
        else if (_netState.Value == CookingTool.State.Cooked)
        {
            return _burningRecipeSO != null ? _netBurningTimer.Value / _burningTimeMax : 1f;
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

        KitchenObjectSO input = _kitchenObject != null ? _kitchenObject.GetKitchenObjectSO() : null;
        if (input == null)
            return;

        List<KitchenObjectSO> options = GetListKitchenObjectList(input);
        if (index < 0 || index >= options.Count)
            return;

        ApplyCombineRecipe(input, options[index]);
        SetOptionKitchenObjectServerRpc(index);
    }

    [Rpc(SendTo.Server)]
    private void SetOptionKitchenObjectServerRpc(int index)
    {
        _netCombineRecipeIndex.Value = index;
    }

    private void ApplyCombineRecipe(KitchenObjectSO input, KitchenObjectSO chosenOutput)
    {
        _combineRecipeSO = null;
        var combineList = RecipeDatabase?.GetCombineRecipesForInput(input);
        if (combineList != null)
        {
            foreach (CombineRecipeSO combineRecipe in combineList)
            {
                if (combineRecipe.output == chosenOutput)
                {
                    _combineRecipeSO = combineRecipe;
                    break;
                }
            }
        }

        _cookingTimeMax = _combineRecipeSO != null ? _combineRecipeSO.combineTimerMax : 0f;
        _recipeResolved = _cookingTimeMax > 0f;

        if (combineDetailUI != null)
            combineDetailUI.InitUI(_combineRecipeSO);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        List<KitchenObjectSO> kitchenObjectSOs = new List<KitchenObjectSO>();
        if (Config?.toolType != CookingToolConfigSO.CookingToolType.Combine)
            return kitchenObjectSOs;

        var combineList = RecipeDatabase?.GetCombineRecipesForInput(kitchenObjectSO);
        if (combineList == null)
            return kitchenObjectSOs;

        foreach (CombineRecipeSO combineRecipe in combineList)
            kitchenObjectSOs.Add(combineRecipe.output);

        return kitchenObjectSOs;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}
