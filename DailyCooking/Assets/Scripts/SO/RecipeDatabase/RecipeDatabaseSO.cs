using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "SO/RecipeDatabase")]
public class RecipeDatabaseSO : ScriptableObject
{
    [SerializeField] private List<CuttingRecipeSO> cuttingRecipes;
    [SerializeField] private List<FryingRecipeSO> fryingRecipes;
    [SerializeField] private List<BurningRecipeSO> burningRecipes;
    [SerializeField] private List<CombineRecipeSO> combineRecipes;
    [SerializeField] private List<BakingRecipeSO> bakingRecipes;
    [SerializeField] private List<DeepFryRecipeSO> deepFryRecipes;
    [SerializeField] private List<DrinkRecipeSO> drinkRecipes;

    private Dictionary<KitchenObjectSO, List<CuttingRecipeSO>> _cuttingCache;
    private Dictionary<KitchenObjectSO, FryingRecipeSO> _fryingCache;
    private Dictionary<KitchenObjectSO, BurningRecipeSO> _burningCache;
    private Dictionary<KitchenObjectSO, List<CombineRecipeSO>> _combineCache;
    private Dictionary<KitchenObjectSO, BakingRecipeSO> _bakingCache;
    private Dictionary<KitchenObjectSO, DeepFryRecipeSO> _deepFryCache;
    private List<DrinkRecipeSO> _drinkCache;

    public void Initialize()
    {
        // One input may offer several outputs (Potato -> Chopped or Mashed). They are
        // selected through the cutting option menu; only the same input/output pair is
        // treated as a data error.
        _cuttingCache = new Dictionary<KitchenObjectSO, List<CuttingRecipeSO>>();
        foreach (var recipe in cuttingRecipes)
        {
            if (recipe == null || recipe.input == null || recipe.output == null)
                continue;
            if (!_cuttingCache.TryGetValue(recipe.input, out var list))
            {
                list = new List<CuttingRecipeSO>();
                _cuttingCache[recipe.input] = list;
            }
            if (list.Exists(existing => existing.output == recipe.output))
            {
                Debug.LogError($"[RecipeDatabase] Duplicate cutting recipe '{recipe.input.name} -> {recipe.output.name}'; keeping the first entry.");
                continue;
            }
            list.Add(recipe);
        }

        _fryingCache = new Dictionary<KitchenObjectSO, FryingRecipeSO>();
        foreach (var recipe in fryingRecipes)
        {
            _fryingCache[recipe.input] = recipe;
        }

        _burningCache = new Dictionary<KitchenObjectSO, BurningRecipeSO>();
        foreach (var recipe in burningRecipes)
        {
            _burningCache[recipe.input] = recipe;
        }

        _combineCache = new Dictionary<KitchenObjectSO, List<CombineRecipeSO>>();
        foreach (var recipe in combineRecipes)
        {
            foreach (var input in recipe.input)
            {
                if (!_combineCache.TryGetValue(input, out var list))
                {
                    list = new List<CombineRecipeSO>();
                    _combineCache[input] = list;
                }
                list.Add(recipe);
            }
        }

        _bakingCache = new Dictionary<KitchenObjectSO, BakingRecipeSO>();
        foreach (var recipe in bakingRecipes)
        {
            _bakingCache[recipe.input] = recipe;
        }

        _deepFryCache = new Dictionary<KitchenObjectSO, DeepFryRecipeSO>();
        foreach (var recipe in deepFryRecipes)
        {
            _deepFryCache[recipe.input] = recipe;
        }

        _drinkCache = new List<DrinkRecipeSO>(drinkRecipes);
    }


    public CuttingRecipeSO GetCuttingRecipe(KitchenObjectSO input)
    {
        if (input != null && _cuttingCache.TryGetValue(input, out var recipes) && recipes.Count > 0)
            return recipes[0];
        return null;
    }

    public List<CuttingRecipeSO> GetCuttingRecipesForInput(KitchenObjectSO input)
    {
        if (input == null || !_cuttingCache.TryGetValue(input, out var recipes))
            return null;
        return recipes;
    }

    public FryingRecipeSO GetFryingRecipe(KitchenObjectSO input)
    {
        _fryingCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public BurningRecipeSO GetBurningRecipe(KitchenObjectSO input)
    {
        _burningCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public CombineRecipeSO GetCombineRecipe(KitchenObjectSO input)
    {
        if (_combineCache.TryGetValue(input, out var list) && list.Count > 0)
            return list[0];
        return null;
    }

    public List<CombineRecipeSO> GetCombineRecipesForInput(KitchenObjectSO input)
    {
        if (input == null || !_combineCache.TryGetValue(input, out var list))
            return null;
        return list;
    }

    // Multi-input combine lookups (the Pot's Stew). Like drinks, a combine recipe resolves
    // only when the whole ingredient set matches, and an item may be added while some
    // recipe still fits.
    public CombineRecipeSO GetCombineRecipeForIngredients(IReadOnlyList<KitchenObjectSO> ingredients)
    {
        List<CombineRecipeSO> recipes = GetCombineRecipesForIngredients(ingredients);
        return recipes.Count > 0 ? recipes[0] : null;
    }

    public List<CombineRecipeSO> GetCombineRecipesForIngredients(IReadOnlyList<KitchenObjectSO> ingredients)
    {
        List<CombineRecipeSO> matches = new List<CombineRecipeSO>();
        if (ingredients == null || ingredients.Count == 0 || combineRecipes == null)
            return matches;
        for (int i = 0; i < combineRecipes.Count; i++)
        {
            CombineRecipeSO recipe = combineRecipes[i];
            if (recipe == null || recipe.input == null || recipe.input.Count == 0)
                continue;
            if (IngredientsMatch(recipe.input, ingredients))
                matches.Add(recipe);
        }
        return matches;
    }

    // True when `candidate` can still be added to `current` and some combine recipe remains possible.
    public bool CanAcceptCombineIngredient(IReadOnlyList<KitchenObjectSO> current, KitchenObjectSO candidate)
    {
        if (candidate == null || combineRecipes == null)
            return false;

        List<KitchenObjectSO> withCandidate = new List<KitchenObjectSO>((current != null ? current.Count : 0) + 1);
        if (current != null)
            withCandidate.AddRange(current);
        withCandidate.Add(candidate);

        for (int i = 0; i < combineRecipes.Count; i++)
        {
            CombineRecipeSO recipe = combineRecipes[i];
            if (recipe == null || recipe.input == null || recipe.input.Count == 0)
                continue;
            if (withCandidate.Count > recipe.input.Count)
                continue;
            if (IsSubset(recipe.input, withCandidate))
                return true;
        }
        return false;
    }

    public BakingRecipeSO GetBakingRecipe(KitchenObjectSO input)
    {
        _bakingCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public DeepFryRecipeSO GetDeepFryRecipe(KitchenObjectSO input)
    {
        _deepFryCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    // Multi-input lookups. A tool may hold several ingredients (the Blender), so a drink
    // recipe is resolved against the whole set, not against each ingredient on its own.
    public DrinkRecipeSO GetDrinkRecipeForIngredients(IReadOnlyList<KitchenObjectSO> ingredients)
    {
        if (ingredients == null || ingredients.Count == 0)
            return null;
        EnsureDrinkCaches();
        for (int i = 0; i < _drinkCache.Count; i++)
        {
            DrinkRecipeSO recipe = _drinkCache[i];
            if (recipe == null || recipe.input == null || recipe.input.Count == 0)
                continue;
            if (IngredientsMatch(recipe.input, ingredients))
                return recipe;
        }
        return null;
    }

    // A served drink is a single item, so its FoodSO lists the drink itself. Menus want the
    // ingredients that went into it instead, which they can look up from the drink output.
    public DrinkRecipeSO GetDrinkRecipeForOutput(KitchenObjectSO output)
    {
        if (output == null)
            return null;
        EnsureDrinkCaches();
        for (int i = 0; i < _drinkCache.Count; i++)
        {
            DrinkRecipeSO recipe = _drinkCache[i];
            if (recipe != null && recipe.output == output)
                return recipe;
        }
        return null;
    }

    // True when `candidate` can still be added to `current` and some drink recipe remains possible.
    public bool CanAcceptDrinkIngredient(IReadOnlyList<KitchenObjectSO> current, KitchenObjectSO candidate)
    {
        if (candidate == null)
            return false;
        EnsureDrinkCaches();

        List<KitchenObjectSO> withCandidate = new List<KitchenObjectSO>((current != null ? current.Count : 0) + 1);
        if (current != null)
            withCandidate.AddRange(current);
        withCandidate.Add(candidate);

        for (int i = 0; i < _drinkCache.Count; i++)
        {
            DrinkRecipeSO recipe = _drinkCache[i];
            if (recipe == null || recipe.input == null || recipe.input.Count == 0)
                continue;
            if (withCandidate.Count > recipe.input.Count)
                continue;
            if (IsSubset(recipe.input, withCandidate))
                return true;
        }
        return false;
    }

    // Order independent multiset comparison (the same ingredient may appear more than once).
    public static bool IngredientsMatch(IReadOnlyList<KitchenObjectSO> recipeInputs, IReadOnlyList<KitchenObjectSO> ingredients)
    {
        if (recipeInputs == null || ingredients == null || recipeInputs.Count != ingredients.Count)
            return false;
        return IsSubset(recipeInputs, ingredients);
    }

    // True when every entry of `ingredients` is available in `recipeInputs`, counting duplicates.
    public static bool IsSubset(IReadOnlyList<KitchenObjectSO> recipeInputs, IReadOnlyList<KitchenObjectSO> ingredients)
    {
        if (recipeInputs == null || ingredients == null || ingredients.Count > recipeInputs.Count)
            return false;

        bool[] used = new bool[recipeInputs.Count];
        for (int i = 0; i < ingredients.Count; i++)
        {
            KitchenObjectSO ingredient = ingredients[i];
            if (ingredient == null)
                return false;
            bool found = false;
            for (int j = 0; j < recipeInputs.Count; j++)
            {
                if (used[j] || recipeInputs[j] != ingredient)
                    continue;
                used[j] = true;
                found = true;
                break;
            }
            if (!found)
                return false;
        }
        return true;
    }

    private void EnsureDrinkCaches()
    {
        // Lookups can run before KitchenGameManager calls Initialize() (EditMode tests,
        // a prefab inspected in the editor), so build the caches on first use.
        if (_drinkCache == null)
            Initialize();
    }
}