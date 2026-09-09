using System;
using System.Collections.Generic;

// Pure recipe logic. No MonoBehaviour, no UI except via CombineResolved callback.
// Callers read CookingOutput / BurningOutput instead of facade GetCookingOutput().
public sealed class RecipeResolver
{
    private CookingToolConfigSO _config;
    private IReadOnlyList<CookingToolConfigSO.CookingToolType> _types =
        Array.Empty<CookingToolConfigSO.CookingToolType>();
    private RecipeDatabaseSO _db;

    private FryingRecipeSO _frying;
    private BakingRecipeSO _baking;
    private DeepFryRecipeSO _deepFry;
    private DrinkRecipeSO _drink;
    private CombineRecipeSO _combine;
    private BurningRecipeSO _burning;

    private bool _lookupDone;
    private bool _recipeResolved;
    private bool _burningResolved;
    private int _appliedCombineIndex = -2;

    public event Action<CombineRecipeSO> CombineResolved;

    public float CookingTimeMax { get; private set; }
    public float BurningTimeMax { get; private set; }
    public bool HasValidRecipe => CookingTimeMax > 0f;
    public int AppliedCombineIndex => _appliedCombineIndex;

    public KitchenObjectSO CookingOutput
    {
        get
        {
            if (_frying != null) return _frying.output;
            if (_baking != null) return _baking.output;
            if (_deepFry != null) return _deepFry.output;
            if (_drink != null) return _drink.output;
            if (_combine != null) return _combine.output;
            return null;
        }
    }

    public KitchenObjectSO BurningOutput => _burning != null ? _burning.output : null;
    public BurningRecipeSO BurningRecipe => _burning;

    public void SetConfig(CookingToolConfigSO config)
    {
        _config = config;
        _types = config != null && config.toolTypes != null
            ? config.toolTypes
            : Array.Empty<CookingToolConfigSO.CookingToolType>();
    }

    public void SetDatabase(RecipeDatabaseSO db)
    {
        _db = db;
    }

    public void ResetSlot()
    {
        _frying = null;
        _baking = null;
        _deepFry = null;
        _drink = null;
        _combine = null;
        CookingTimeMax = 0f;
        _lookupDone = false;
        _recipeResolved = false;
        _burningResolved = false;
        _appliedCombineIndex = -2;
    }

    public void ResetCombine()
    {
        _appliedCombineIndex = -2;
    }

    public bool Supports(CookingToolConfigSO.CookingToolType type)
    {
        return _config != null && _config.Supports(type);
    }

    public bool HasRecipe(KitchenObjectSO input)
    {
        if (_config == null || input == null || _db == null)
            return false;
        for (int i = 0; i < _types.Count; i++)
        {
            switch (_types[i])
            {
                case CookingToolConfigSO.CookingToolType.Frying:
                    if (_db.GetFryingRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Baking:
                    if (_db.GetBakingRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.DeepFry:
                    if (_db.GetDeepFryRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Beverage:
                    if (_db.GetDrinkRecipeByIngredient(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Combine:
                    if (_db.GetCombineRecipe(input) != null) return true;
                    break;
            }
        }
        return false;
    }

    public bool RequiresOption(KitchenObjectSO input, bool supportsMenu)
    {
        if (_config == null || !supportsMenu || input == null || _db == null)
            return false;
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return false;
        return _db.GetCombineRecipe(input) != null;
    }

    public void ResolveFor(KitchenObjectSO input)
    {
        _frying = null;
        _baking = null;
        _deepFry = null;
        _drink = null;
        CookingTimeMax = 0f;
        _lookupDone = true;
        _recipeResolved = false;
        if (_config == null || input == null || _db == null)
            return;
        for (int i = 0; i < _types.Count; i++)
        {
            switch (_types[i])
            {
                case CookingToolConfigSO.CookingToolType.Frying:
                    _frying = _db.GetFryingRecipe(input);
                    if (_frying != null) { CookingTimeMax = _frying.fryingTimerMax; _recipeResolved = true; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Baking:
                    _baking = _db.GetBakingRecipe(input);
                    if (_baking != null) { CookingTimeMax = _baking.bakingTimerMax; _recipeResolved = true; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.DeepFry:
                    _deepFry = _db.GetDeepFryRecipe(input);
                    if (_deepFry != null) { CookingTimeMax = _deepFry.deepFryTimerMax; _recipeResolved = true; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Beverage:
                    _drink = _db.GetDrinkRecipeByIngredient(input);
                    if (_drink != null) { CookingTimeMax = _drink.drinkTimerMax; _recipeResolved = true; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Combine:
                    break;
            }
        }
    }

    public void EnsureResolved(KitchenObjectSO input)
    {
        if (_lookupDone)
            return;
        ResolveFor(input);
    }

    public void ResolveBurning(KitchenObjectSO input)
    {
        _burning = input != null && _db != null ? _db.GetBurningRecipe(input) : null;
        BurningTimeMax = _burning != null ? _burning.burningTimerMax : 0f;
    }

    public void EnsureBurning(KitchenObjectSO input)
    {
        if (_burningResolved || input == null)
            return;
        ResolveBurning(input);
        _burningResolved = true;
    }

    public void MarkBurningResolved()
    {
        _burningResolved = true;
    }

    // Non-alloc hot path: index directly into DB list. Returns false if nothing applied.
    public bool EnsureCombine(KitchenObjectSO input, int netIndex)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return false;
        if (netIndex < 0 || netIndex == _appliedCombineIndex)
            return false;
        if (input == null || _db == null)
            return false;
        var list = _db.GetCombineRecipesForInput(input);
        if (list == null || (uint)netIndex >= (uint)list.Count)
            return false;
        KitchenObjectSO chosen = list[netIndex].output;
        if (chosen == null)
            return false;
        _appliedCombineIndex = netIndex;
        ApplyCombine(input, chosen);
        return true;
    }

    public bool ApplyOption(KitchenObjectSO input, int index)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return false;
        if (input == null || _db == null)
            return false;
        var list = _db.GetCombineRecipesForInput(input);
        if (list == null || (uint)index >= (uint)list.Count)
            return false;
        _appliedCombineIndex = index;
        ApplyCombine(input, list[index].output);
        return true;
    }

    private void ApplyCombine(KitchenObjectSO input, KitchenObjectSO chosenOutput)
    {
        _combine = null;
        var list = _db != null ? _db.GetCombineRecipesForInput(input) : null;
        if (list != null)
        {
            foreach (CombineRecipeSO r in list)
            {
                if (r.output == chosenOutput)
                {
                    _combine = r;
                    break;
                }
            }
        }
        CookingTimeMax = _combine != null ? _combine.combineTimerMax : 0f;
        _lookupDone = true;
        _recipeResolved = CookingTimeMax > 0f;
        CombineResolved?.Invoke(_combine);
    }

    public List<KitchenObjectSO> GetOptions(KitchenObjectSO input)
    {
        var result = new List<KitchenObjectSO>();
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return result;
        var list = _db != null ? _db.GetCombineRecipesForInput(input) : null;
        if (list == null)
            return result;
        foreach (CombineRecipeSO r in list)
            result.Add(r.output);
        return result;
    }
}
