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

    private Dictionary<KitchenObjectSO, CuttingRecipeSO> _cuttingCache;
    private Dictionary<KitchenObjectSO, FryingRecipeSO> _fryingCache;
    private Dictionary<KitchenObjectSO, BurningRecipeSO> _burningCache;
    private Dictionary<KitchenObjectSO, CombineRecipeSO> _combineCache;

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
    }

    public CuttingRecipeSO[] GetCuttingRecipes() => cuttingRecipes?.ToArray();
    public FryingRecipeSO[] GetFryingRecipes() => fryingRecipes?.ToArray();
    public BurningRecipeSO[] GetBurningRecipes() => burningRecipes?.ToArray();
    public CombineRecipeSO[] GetCombineRecipes() => combineRecipes?.ToArray();

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
}