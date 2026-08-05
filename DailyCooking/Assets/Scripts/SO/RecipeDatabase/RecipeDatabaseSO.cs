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
    private Dictionary<KitchenObjectSO, CombineRecipeSO> _combineCache;
    private Dictionary<KitchenObjectSO, BakingRecipeSO> _bakingCache;
    private Dictionary<KitchenObjectSO, DeepFryRecipeSO> _deepFryCache;
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

        _combineCache = new Dictionary<KitchenObjectSO, CombineRecipeSO>();
        foreach (var recipe in combineRecipes)
        {
            foreach (var input in recipe.input)
            {
                _combineCache[input] = recipe;
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

    public CuttingRecipeSO[] GetCuttingRecipes() => cuttingRecipes?.ToArray();
    public FryingRecipeSO[] GetFryingRecipes() => fryingRecipes?.ToArray();
    public BurningRecipeSO[] GetBurningRecipes() => burningRecipes?.ToArray();
    public CombineRecipeSO[] GetCombineRecipes() => combineRecipes?.ToArray();
    public BakingRecipeSO[] GetBakingRecipes() => bakingRecipes?.ToArray();
    public DeepFryRecipeSO[] GetDeepFryRecipes() => deepFryRecipes?.ToArray();
    public DrinkRecipeSO[] GetDrinkRecipes() => drinkRecipes?.ToArray();

    public CuttingRecipeSO GetCuttingRecipe(KitchenObjectSO input)
    {
        if (_cuttingCache == null) Initialize();
        _cuttingCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public FryingRecipeSO GetFryingRecipe(KitchenObjectSO input)
    {
        if (_fryingCache == null) Initialize();
        _fryingCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public BurningRecipeSO GetBurningRecipe(KitchenObjectSO input)
    {
        if (_burningCache == null) Initialize();
        _burningCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public CombineRecipeSO GetCombineRecipe(KitchenObjectSO input)
    {
        if (_combineCache == null) Initialize();
        _combineCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public BakingRecipeSO GetBakingRecipe(KitchenObjectSO input)
    {
        if (_bakingCache == null) Initialize();
        _bakingCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public DeepFryRecipeSO GetDeepFryRecipe(KitchenObjectSO input)
    {
        if (_deepFryCache == null) Initialize();
        _deepFryCache.TryGetValue(input, out var recipe);
        return recipe;
    }

    public DrinkRecipeSO GetDrinkRecipe(List<KitchenObjectSO> inputs)
    {
        if (_drinkCache == null) Initialize();
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