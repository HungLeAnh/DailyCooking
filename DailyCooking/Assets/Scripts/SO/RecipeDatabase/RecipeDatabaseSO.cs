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

    private Dictionary<KitchenObjectSO, CuttingRecipeSO> _cuttingCache;
    private Dictionary<KitchenObjectSO, FryingRecipeSO> _fryingCache;
    private Dictionary<KitchenObjectSO, BurningRecipeSO> _burningCache;
    private Dictionary<KitchenObjectSO, List<CombineRecipeSO>> _combineCache;
    private Dictionary<KitchenObjectSO, BakingRecipeSO> _bakingCache;
    private Dictionary<KitchenObjectSO, DeepFryRecipeSO> _deepFryCache;
    private Dictionary<KitchenObjectSO, List<DrinkRecipeSO>> _drinkIngredientCache;
    private List<DrinkRecipeSO> _drinkCache;

    public void Initialize()
    {
        _cuttingCache = new Dictionary<KitchenObjectSO, CuttingRecipeSO>();
        foreach (var recipe in cuttingRecipes)
        {
            _cuttingCache[recipe.input] = recipe;
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

        _drinkIngredientCache = new Dictionary<KitchenObjectSO, List<DrinkRecipeSO>>();
        _drinkCache = new List<DrinkRecipeSO>(drinkRecipes);
        foreach (var recipe in drinkRecipes)
        {
            foreach (var input in recipe.input)
            {
                if (!_drinkIngredientCache.TryGetValue(input, out var list))
                {
                    list = new List<DrinkRecipeSO>();
                    _drinkIngredientCache[input] = list;
                }
                list.Add(recipe);
            }
        }
    }


    public CuttingRecipeSO GetCuttingRecipe(KitchenObjectSO input)
    {
        _cuttingCache.TryGetValue(input, out var recipe);
        return recipe;
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

    public DrinkRecipeSO GetDrinkRecipeByIngredient(KitchenObjectSO input)
    {
        if (_drinkIngredientCache.TryGetValue(input, out var list) && list.Count > 0)
            return list[0];
        return null;
    }

    public DrinkRecipeSO GetDrinkRecipe(List<KitchenObjectSO> inputs)
    {
        if (inputs == null || inputs.Count == 0) return null;
        foreach (var recipe in _drinkCache)
        {
            if (recipe.input.Count != inputs.Count) continue;
            bool allMatch = true;
            foreach (var need in recipe.input)
            {
                if (!inputs.Contains(need))
                {
                    allMatch = false;
                    break;
                }
            }
            if (allMatch) return recipe;
        }
        return null;
    }
}