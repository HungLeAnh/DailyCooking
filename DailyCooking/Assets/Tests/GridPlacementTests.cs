using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GridPlacementTests
{
    private readonly List<Object> createdObjects = new List<Object>();

    [TearDown] public void TearDown()
    {
        foreach (Object createdObject in createdObjects)
            Object.DestroyImmediate(createdObject);
        createdObjects.Clear();
    }

    private static GridXZ<GridObject> CreateGrid(int size)
    {
        var grid = new GridXZ<GridObject>(0, 0, 2f, Vector3.zero,
            (g, x, z) => new List<GridObject> { new GridObject(g, x, z) });
        grid.SetSize(size, size, size, size);
        return grid;
    }

    private PlacedObjectTypeSO CreateCounterSO(int width, int height)
    {
        var placedObjectTypeSO = ScriptableObject.CreateInstance<PlacedObjectTypeSO>();
        placedObjectTypeSO.width = width;
        placedObjectTypeSO.height = height;
        placedObjectTypeSO.itemType = new ItemType { TabType = InventoryTabType.Counter };
        createdObjects.Add(placedObjectTypeSO);
        return placedObjectTypeSO;
    }

    [Test] public void TryGetXZ_ReportsPositionsOutsideTheGrid()
    {
        var grid = CreateGrid(5);
        Assert.IsTrue(grid.TryGetXZ(new Vector3(4f, 0f, 6f), out int x, out int z));
        Assert.AreEqual(2, x);
        Assert.AreEqual(3, z);
        Assert.IsFalse(grid.TryGetXZ(new Vector3(-4f, 0f, 0f), out _, out _));
        Assert.IsFalse(grid.TryGetXZ(new Vector3(0f, 0f, 20f), out _, out _));
    }

    [Test] public void IsCellUnlocked_MatchesFloorRule()
    {
        var grid = CreateGrid(10);
        // Expanded area not bought yet: rows z >= 5 only unlocked for x < 5.
        grid.SetSize(5, 5, 10, 10);

        Assert.IsTrue(grid.IsCellUnlocked(9, 4));
        Assert.IsTrue(grid.IsCellUnlocked(4, 9));
        Assert.IsFalse(grid.IsCellUnlocked(5, 5));
        Assert.IsFalse(grid.IsCellUnlocked(10, 0));
    }

    [Test] public void SetSize_GrowsArrayAndKeepsCells()
    {
        var grid = CreateGrid(3);
        List<GridObject> cell = grid.GetGridObject(1, 1);

        grid.SetSize(6, 6, 6, 6);

        Assert.AreSame(cell, grid.GetGridObject(1, 1));
        Assert.IsNotNull(grid.GetGridObject(5, 5));
    }

    [Test] public void CanPlace_OnEmptyUnlockedCells()
    {
        var grid = CreateGrid(5);
        Assert.IsTrue(PlacementRules.CanPlace(grid, CreateCounterSO(2, 1), new Vector2Int(1, 1), Dir.Down, requireUnlockedCells: true));
    }

    [Test] public void CanPlace_RejectsFootprintOutsideTheGrid()
    {
        var grid = CreateGrid(5);
        Assert.IsFalse(PlacementRules.CanPlace(grid, CreateCounterSO(2, 1), new Vector2Int(4, 0), Dir.Down, requireUnlockedCells: false));
    }

    [Test] public void CanPlace_RejectsLockedCellsOnlyForPlayers()
    {
        var grid = CreateGrid(10);
        grid.SetSize(5, 5, 10, 10);
        var counter = CreateCounterSO(1, 1);

        Assert.IsFalse(PlacementRules.CanPlace(grid, counter, new Vector2Int(7, 7), Dir.Down, requireUnlockedCells: true));
        Assert.IsTrue(PlacementRules.CanPlace(grid, counter, new Vector2Int(7, 7), Dir.Down, requireUnlockedCells: false));
    }

    [Test] public void CanPlace_RejectsNullType()
    {
        Assert.IsFalse(PlacementRules.CanPlace(CreateGrid(5), null, Vector2Int.zero, Dir.Down, requireUnlockedCells: true));
    }
}
