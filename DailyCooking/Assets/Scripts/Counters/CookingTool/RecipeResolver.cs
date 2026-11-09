using System;
using System.Collections.Generic;

// Pure recipe logic. No MonoBehaviour, no UI except via CombineResolved callback.
// Callers read CookingOutput / BurningOutput instead of facade GetCookingOutput().
public sealed class RecipeResolver
{
    private CookingToolConfigSO _config;
    private IReadOnlyList<CookingToolConfigSO.CookingToolType> _types =
        Array.Empty<CookingToolConfigSO.CookingToolType>();

    private FryingRecipeSO _frying;
    private BakingRecipeSO _baking;
    private DeepFryRecipeSO _deepFry;
    private DrinkRecipeSO _drink;
    private CombineRecipeSO _combine;
    private CuttingRecipeSO _cutting;
    private List<CuttingRecipeSO> _cuttingOptions;
    private BurningRecipeSO _burning;

    private bool _lookupDone;
    private bool _burningResolved;
    private int _appliedCombineIndex = -2;

    // Reused for the single-input overloads so hot paths stay allocation free.
    private readonly KitchenObjectSO[] _singleInput = new KitchenObjectSO[1];

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
            if (_cutting != null) return _cutting.output;
            return null;
        }
    }

    public CuttingRecipeSO CuttingRecipe => _cutting;
    public bool IsCutting => _cutting != null;

    public KitchenObjectSO BurningOutput => _burning != null ? _burning.output : null;
    public BurningRecipeSO BurningRecipe => _burning;

    public void SetConfig(CookingToolConfigSO config)
    {
        _config = config;
        _types = config != null && config.toolTypes != null
            ? config.toolTypes
            : Array.Empty<CookingToolConfigSO.CookingToolType>();
    }


    public void ResetSlot()
    {
        _frying = null;
        _baking = null;
        _deepFry = null;
        _drink = null;
        _combine = null;
        _cutting = null;
        _cuttingOptions = null;
        _burning = null;
        CookingTimeMax = 0f;
        BurningTimeMax = 0f;
        _lookupDone = false;
        _burningResolved = false;
        _appliedCombineIndex = -2;
    }

    public bool Supports(CookingToolConfigSO.CookingToolType type)
    {
        return _config != null && _config.Supports(type);
    }

    // Optional per-tool ingredient whitelist. An empty whitelist accepts everything, so
    // tools that do not restrict their inputs (the Blender) behave exactly as before.
    public bool AllowsIngredient(KitchenObjectSO ingredient)
    {
        return _config == null || _config.AllowsIngredient(ingredient);
    }

    private bool AllowsAllInputs(IReadOnlyList<KitchenObjectSO> inputs)
    {
        if (_config == null || _config.allowedIngredients == null || _config.allowedIngredients.Count == 0)
            return true;
        for (int i = 0; i < inputs.Count; i++)
        {
            if (!_config.AllowsIngredient(inputs[i]))
                return false;
        }
        return true;
    }

    public bool HasRecipe(KitchenObjectSO input)
    {
        if (input == null)
            return false;
        _singleInput[0] = input;
        return HasRecipe(_singleInput);
    }

    // True when the current ingredient set can still produce a recipe (exact match for
    // single-input tools, complete set for multi-input tools such as the Blender).
    public bool HasRecipe(IReadOnlyList<KitchenObjectSO> inputs)
    {
        if (_config == null || inputs == null || inputs.Count == 0 || KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        KitchenObjectSO firstInput = inputs[0];
        if (firstInput == null)
            return false;
        // Single-input recipe types only make sense when the tool holds one item.
        bool singleInput = inputs.Count == 1;
        for (int i = 0; i < _types.Count; i++)
        {
            switch (_types[i])
            {
                case CookingToolConfigSO.CookingToolType.Frying:
                    if (singleInput && KitchenGameManager.Instance.RecipeDatabase.GetFryingRecipe(firstInput) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Baking:
                    if (singleInput && KitchenGameManager.Instance.RecipeDatabase.GetBakingRecipe(firstInput) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.DeepFry:
                    if (singleInput && KitchenGameManager.Instance.RecipeDatabase.GetDeepFryRecipe(firstInput) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Beverage:
                    if (AllowsAllInputs(inputs) &&
                        KitchenGameManager.Instance.RecipeDatabase.GetDrinkRecipeForIngredients(inputs) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Combine:
                    if (singleInput && KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipe(firstInput) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Cutting:
                    if (singleInput && KitchenGameManager.Instance.RecipeDatabase.GetCuttingRecipe(firstInput) != null) return true;
                    break;
            }
        }
        return false;
    }

    // Empty-slot acceptance: can this ingredient start some recipe on this tool? A
    // multi-input Combine recipe counts as soon as the ingredient is one of its inputs,
    // which is what lets the Pot take the first half of a Stew.
    public bool CanStartWith(KitchenObjectSO input)
    {
        if (input == null || _config == null || KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        RecipeDatabaseSO database = KitchenGameManager.Instance.RecipeDatabase;
        for (int i = 0; i < _types.Count; i++)
        {
            switch (_types[i])
            {
                case CookingToolConfigSO.CookingToolType.Frying:
                    if (database.GetFryingRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Baking:
                    if (database.GetBakingRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.DeepFry:
                    if (database.GetDeepFryRecipe(input) != null) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Beverage:
                    if (AllowsIngredient(input) &&
                        database.CanAcceptDrinkIngredient(Array.Empty<KitchenObjectSO>(), input)) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Combine:
                    List<CombineRecipeSO> combines = database.GetCombineRecipesForInput(input);
                    if (combines != null && combines.Count > 0) return true;
                    break;
                case CookingToolConfigSO.CookingToolType.Cutting:
                    if (database.GetCuttingRecipe(input) != null) return true;
                    break;
            }
        }
        return false;
    }

    // Blender only: can this ingredient still be added to the current set without wasting it?
    public bool CanAcceptDrinkIngredient(IReadOnlyList<KitchenObjectSO> current, KitchenObjectSO candidate)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Beverage) || candidate == null ||
            !AllowsIngredient(candidate) ||
            KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        return KitchenGameManager.Instance.RecipeDatabase.CanAcceptDrinkIngredient(current, candidate);
    }

    public bool CanAcceptCombineIngredient(IReadOnlyList<KitchenObjectSO> current, KitchenObjectSO candidate)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine) || candidate == null ||
            KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        return KitchenGameManager.Instance.RecipeDatabase.CanAcceptCombineIngredient(current, candidate);
    }

    // Combine tools only need the option menu when the complete ingredient set can
    // produce more than one dish. A half-filled Pot must never pop the menu.
    public bool RequiresCombineChoice(IReadOnlyList<KitchenObjectSO> inputs, bool supportsMenu)
    {
        if (_config == null || !supportsMenu ||
            !Supports(CookingToolConfigSO.CookingToolType.Combine) ||
            inputs == null || inputs.Count == 0 ||
            KitchenGameManager.Instance.RecipeDatabase == null)
            return false;

        List<CombineRecipeSO> recipes = KitchenGameManager.Instance.RecipeDatabase
            .GetCombineRecipesForIngredients(inputs);
        if (recipes.Count < 2)
            return false;

        KitchenObjectSO firstOutput = recipes[0] != null ? recipes[0].output : null;
        for (int i = 1; i < recipes.Count; i++)
        {
            if (recipes[i] != null && recipes[i].output != firstOutput)
                return true;
        }
        return false;
    }

    public void ResolveFor(KitchenObjectSO input)
    {
        if (input == null)
        {
            ResolveFor(Array.Empty<KitchenObjectSO>());
            return;
        }
        _singleInput[0] = input;
        ResolveFor(_singleInput);
    }

    // Multi-input tools resolve the recipe from the whole ingredient set. Single-input
    // recipe types keep using the first item, which is all those tools can hold.
    public void ResolveFor(IReadOnlyList<KitchenObjectSO> inputs)
    {
        _frying = null;
        _baking = null;
        _deepFry = null;
        _drink = null;
        _combine = null;
        _cutting = null;
        _cuttingOptions = null;
        CookingTimeMax = 0f;
        _lookupDone = true;
        if (_config == null || inputs == null || inputs.Count == 0 || KitchenGameManager.Instance.RecipeDatabase == null)
            return;

        KitchenObjectSO firstInput = inputs[0];
        if (firstInput == null)
            return;
        // Single-input recipe types only make sense when the tool holds one item.
        bool singleInput = inputs.Count == 1;

        for (int i = 0; i < _types.Count; i++)
        {
            switch (_types[i])
            {
                case CookingToolConfigSO.CookingToolType.Frying:
                    if (!singleInput) break;
                    _frying = KitchenGameManager.Instance.RecipeDatabase.GetFryingRecipe(firstInput);
                    if (_frying != null) { CookingTimeMax = _frying.fryingTimerMax; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Baking:
                    if (!singleInput) break;
                    _baking = KitchenGameManager.Instance.RecipeDatabase.GetBakingRecipe(firstInput);
                    if (_baking != null) { CookingTimeMax = _baking.bakingTimerMax; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.DeepFry:
                    if (!singleInput) break;
                    _deepFry = KitchenGameManager.Instance.RecipeDatabase.GetDeepFryRecipe(firstInput);
                    if (_deepFry != null) { CookingTimeMax = _deepFry.deepFryTimerMax; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Beverage:
                    if (!AllowsAllInputs(inputs)) break;
                    _drink = KitchenGameManager.Instance.RecipeDatabase.GetDrinkRecipeForIngredients(inputs);
                    if (_drink != null) { CookingTimeMax = _drink.drinkTimerMax; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Combine:
                    // Whole-set match: a Stew only cooks once both ingredients are inside.
                    _combine = KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipeForIngredients(inputs);
                    if (_combine != null) { CookingTimeMax = _combine.combineTimerMax; return; }
                    break;
                case CookingToolConfigSO.CookingToolType.Cutting:
                    if (!singleInput) break;
                    _cuttingOptions = KitchenGameManager.Instance.RecipeDatabase.GetCuttingRecipesForInput(firstInput);
                    _cutting = _cuttingOptions != null && _cuttingOptions.Count > 0 ? _cuttingOptions[0] : null;
                    if (_cutting != null) { CookingTimeMax = _cutting.cuttingProgressMax; return; }
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

    public void EnsureResolved(IReadOnlyList<KitchenObjectSO> inputs)
    {
        if (_lookupDone)
            return;
        ResolveFor(inputs);
    }

    public List<CuttingRecipeSO> GetCuttingOptions(KitchenObjectSO input)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Cutting) ||
            input == null || KitchenGameManager.Instance.RecipeDatabase == null)
            return null;
        return KitchenGameManager.Instance.RecipeDatabase.GetCuttingRecipesForInput(input);
    }

    public bool HasCuttingOptions(KitchenObjectSO input)
    {
        List<CuttingRecipeSO> options = GetCuttingOptions(input);
        return options != null && options.Count > 0;
    }

    public bool RequiresCuttingChoice(KitchenObjectSO input)
    {
        List<CuttingRecipeSO> options = GetCuttingOptions(input);
        return options != null && options.Count > 1;
    }

    public bool ApplyCuttingOption(KitchenObjectSO input, int index)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Cutting) ||
            input == null || index < 0 || KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        List<CuttingRecipeSO> options =
            KitchenGameManager.Instance.RecipeDatabase.GetCuttingRecipesForInput(input);
        if (options == null || index >= options.Count || options[index] == null)
            return false;
        _frying = null;
        _baking = null;
        _deepFry = null;
        _drink = null;
        _combine = null;
        _cuttingOptions = options;
        _cutting = options[index];
        CookingTimeMax = _cutting.cuttingProgressMax;
        _lookupDone = true;
        return true;
    }

    public void ResolveBurning(KitchenObjectSO input)
    {
        bool canBurn = _config != null && _config.supportsBurning;
        _burning = canBurn && input != null && KitchenGameManager.Instance.RecipeDatabase != null
            ? KitchenGameManager.Instance.RecipeDatabase.GetBurningRecipe(input)
            : null;
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

    // Non-alloc hot path: applies the combine recipe chosen through the option menu.
    // Returns false if nothing changed.
    public bool EnsureCombine(IReadOnlyList<KitchenObjectSO> inputs, int netIndex)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return false;
        if (netIndex < 0 || netIndex == _appliedCombineIndex)
            return false;
        if (inputs == null || inputs.Count == 0 || KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        List<CombineRecipeSO> list = KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipesForIngredients(inputs);
        if ((uint)netIndex >= (uint)list.Count || list[netIndex] == null)
            return false;
        _appliedCombineIndex = netIndex;
        ApplyCombineRecipe(list[netIndex]);
        return true;
    }

    public bool ApplyCombineOption(IReadOnlyList<KitchenObjectSO> inputs, int index)
    {
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return false;
        if (inputs == null || inputs.Count == 0 || KitchenGameManager.Instance.RecipeDatabase == null)
            return false;
        List<CombineRecipeSO> list = KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipesForIngredients(inputs);
        if ((uint)index >= (uint)list.Count || list[index] == null)
            return false;
        _appliedCombineIndex = index;
        ApplyCombineRecipe(list[index]);
        return true;
    }

    private void ApplyCombineRecipe(CombineRecipeSO recipe)
    {
        _combine = recipe;
        CookingTimeMax = _combine != null ? _combine.combineTimerMax : 0f;
        _lookupDone = true;
        CombineResolved?.Invoke(_combine);
    }

    public List<KitchenObjectSO> GetCombineOptionsForIngredients(IReadOnlyList<KitchenObjectSO> inputs)
    {
        var result = new List<KitchenObjectSO>();
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine) ||
            KitchenGameManager.Instance.RecipeDatabase == null)
            return result;
        List<CombineRecipeSO> list = KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipesForIngredients(inputs);
        foreach (CombineRecipeSO r in list)
            if (r != null && r.output != null) result.Add(r.output);
        return result;
    }

    public List<KitchenObjectSO> GetOptions(KitchenObjectSO input)
    {
        var result = new List<KitchenObjectSO>();
        if (!Supports(CookingToolConfigSO.CookingToolType.Combine))
            return result;
        var list = KitchenGameManager.Instance.RecipeDatabase != null ? KitchenGameManager.Instance.RecipeDatabase.GetCombineRecipesForInput(input) : null;
        if (list == null)
            return result;
        foreach (CombineRecipeSO r in list)
            result.Add(r.output);
        return result;
    }
}
