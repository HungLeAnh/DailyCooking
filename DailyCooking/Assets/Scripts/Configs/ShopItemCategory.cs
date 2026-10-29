using UnityEngine;

// Serialized as numbers in scenes, prefabs and config assets: never reorder or renumber,
// only append new values. Values are pinned explicitly so a future insert cannot silently
// shift the ones already written to ConfigShopSO.asset (this project has been bitten by
// exactly that: inserting Ingredient in May 2026 shifted Gem/Coin data in ShopItemType).
//
// Adding a category: append it after `Meats`, e.g. `Drinks = 8`, and keep the number
// below `None`. None is parked at 9999 (it used to be 8; ConfigShopSO.asset was migrated
// in the same commit) exactly so that appending a real category never pushes it out of
// last place.
//
// The pinned numbers are duplicated in the InspectorName labels purely so a designer can
// read them in the Inspector; Unity does not display the numeric value of an enum by
// default. The authoritative copy stays the `= n` below, which BuildChecks verifies.
public enum ShopItemCategory
{
    [InspectorName("Counters (0)")] Counters = 0,
    [InspectorName("Tables (1)")] Tables = 1,
    [InspectorName("Walls (2)")] Walls = 2,
    [InspectorName("Vegetables (3)")] Vegetables = 3,
    [InspectorName("Bakery (4)")] Bakery = 4,
    [InspectorName("Dairy (5)")] Dairy = 5,
    [InspectorName("Patties (6)")] Patties = 6,
    [InspectorName("Meats (7)")] Meats = 7,
    [InspectorName("Tools (8)")] Tools = 8,
    [InspectorName("None (9999)")] None = 9999
}
