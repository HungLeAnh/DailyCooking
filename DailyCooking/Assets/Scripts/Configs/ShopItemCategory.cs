// Serialized as numbers in scenes, prefabs and config assets: never reorder or renumber,
// only append new values. Values are pinned explicitly so a future insert cannot silently
// shift the ones already written to ConfigShopSO.asset (this project has been bitten by
// exactly that: inserting Ingredient in May 2026 shifted Gem/Coin data in ShopItemType).
//
// Adding a category: put it AFTER `None` (e.g. `Drinks = 9`). `None = 8` is already stored
// in assets, so it cannot be moved or renumbered to make room.
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
    None = 8
}
