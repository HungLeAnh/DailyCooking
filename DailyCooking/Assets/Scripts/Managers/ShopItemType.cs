// Serialized as numbers in scenes, prefabs and config assets: never reorder or renumber,
// only append new values. (Inserting Ingredient in May 2026 silently shifted Gem/Coin data.)
public enum ShopItemType
{
    None = 0,
    Item = 1,
    Ingredient = 2,
    Gem = 3,
    Coin = 4,
}
