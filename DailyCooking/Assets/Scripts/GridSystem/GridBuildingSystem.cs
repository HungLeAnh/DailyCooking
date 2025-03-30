using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using JetBrains.Annotations;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;

public class GridBuildingSystem : SimpleSingleton<GridBuildingSystem>
{
    [SerializeField] int gridWidth = 20;
    [SerializeField] int gridHeight = 20;
    [SerializeField] float cellSize = 2f;
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;

    private GridXZ<GridObject> grid;
    private Dir dir = Dir.Down;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private bool isTouchDown;
    private bool isTouchUp;
    private void Start()
    {
        if (GameManager.Instance.GameData.gridData == null)
        {
            grid = new GridXZ<GridObject>(gridWidth, gridHeight, cellSize, Vector3.zero, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));
        }
        else
        {
            grid = new GridXZ<GridObject>(GameManager.Instance.GameData.gridData, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));

        }
            GameInput.Instance.OnTouchPerformed += GameInput_OnTouchPerformed;
        GameInput.Instance.OnFingerUp += GameInput_OnFingerUp;
    }

    private void GameInput_OnTouchPerformed(object sender, UnityEngine.InputSystem.EnhancedTouch.Finger e)
    {
        isTouchDown = true;
    }
    private void GameInput_OnFingerUp(object sender, UnityEngine.InputSystem.EnhancedTouch.Finger e)
    {
        isTouchUp = true;
    }
    private bool CheckTouchInput()
    {
        if (isTouchDown)
        {
            return true;
        }
        return false;
    }
    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame ||
            CheckTouchInput())
        {
            if(GameInput.Instance.IsMouseOverUI()) return;
            if(placedObjectTypeSO == null) return;
            var interactPos = CheckTouchInput()?UtilsClass.GetTouchWorldPosition3D():UtilsClass.GetMouseWorldPosition3D();
            //Debug.LogError($"{interactPos}: ({Mathf.RoundToInt(interactPos.x)},{Mathf.RoundToInt(interactPos.y)},{Mathf.RoundToInt(interactPos.z)})");
            isTouchDown = false;
            grid.GetXZ(new Vector3(Mathf.RoundToInt(interactPos.x),
                                    Mathf.RoundToInt(interactPos.y),
                                    Mathf.RoundToInt(interactPos.z)), out int x, out int z);

            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);

            //Test can build
            bool canBuild = true;
            foreach (var gridPosition in gridPositionList)
            {
                var gridObject = grid.GetGridObject(gridPosition.x, gridPosition.y);
                if (gridObject == null || !gridObject.CanBuild())
                {
                    canBuild = false; 
                    break;
                }
            }

            if (canBuild)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + 
                    new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
                PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, new Vector2Int(x,z) , dir, placedObjectTypeSO);

                foreach (var gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

                }
                GameManager.Instance.GameData.SaveGridData(grid);
            }
            else
            {
                UtilsClass.CreateWorldTextPopup("Can't build here!", interactPos);
            }
        }
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            dir = PlacedObjectTypeSO.GetNextDir(dir);

        }
    }
    public void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO)
    {
        this.placedObjectTypeSO = placedObjectTypeSO;
    }
    public PlacedObjectTypeSO GetPlacedObjectTypeSOByGuid(string Guid)
    {
        var placedObjectSO = placedObjectTypeSOList.Find(x=>x.Guid == Guid);
        if (placedObjectSO != null)
        {
            return placedObjectSO;
        }
        else
        {
            return null;
        }
    }
}

