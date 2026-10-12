// Serialized as numbers in scenes, prefabs and config assets: never reorder or renumber,
// only append new values. Values are pinned explicitly so a future insert cannot silently
// shift the ones already written to ConfigShopSO.asset (this project has been bitten by
// exactly that: inserting Ingredient in May 2026 shifted Gem/Coin data in ShopItemType).
//
// Adding a category: append it after `Meats`, e.g. `Drinks = 8`, and keep the number
// below `None`. None is parked at 9999 (it used to be 8; ConfigShopSO.asset was migrated
// in the same commit) exactly so that appending a real category never pushes it out of
// last place.
public enum ShopItemCategory
{
    Counters = 0,
    Tables = 1,
    Walls = 2,
    Vegetables = 3,
    Bakery = 4,
    Dairy = 5,
    Patties = 6,
    Meats = 7,
    None = 9999
}
