using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class GridInitializer : IGridInitializer
{
    private IGridManager gridManager;
    private IGameManager gameManager;
    private Transform roadContainer;
    private GameObject roadPrefab;
    private GameObject roadCornerPrefab;
    private Transform floorContainer;
    private GameObject floorPrefab;

    public GridInitializer(IGridManager gridManager, IGameManager gameManager, 
        Transform roadContainer, GameObject roadPrefab, GameObject roadCornerPrefab, 
        Transform floorContainer, GameObject floorPrefab)
    {
        this.gridManager = gridManager;
        this.gameManager = gameManager;
        this.roadContainer = roadContainer;
        this.roadPrefab = roadPrefab;
        this.roadCornerPrefab = roadCornerPrefab;
        this.floorContainer = floorContainer;
        this.floorPrefab = floorPrefab;

    }

    public void InitRoad()
    {
        GameObject cornerRoad = GameObject.Instantiate(roadCornerPrefab, gridManager.GetWorldPosition(0, 0), Quaternion.identity);
        cornerRoad.transform.SetParent(roadContainer);

        for (int i = 0; i < 100; i++)
        {
            GameObject road = GameObject.Instantiate(roadPrefab, gridManager.GetWorldPosition(0, i), Quaternion.identity);
            road.transform.SetParent(roadContainer);
            road.transform.rotation = Quaternion.Euler(0, 0, 0);

        }        
        for (int i = 0; i < 100; i++)
        {
            GameObject road = GameObject.Instantiate(roadPrefab, gridManager.GetWorldPosition(i, 0), Quaternion.identity);
            road.transform.SetParent(roadContainer);
            road.transform.rotation = Quaternion.Euler(0, 270, 0);

        }
    }    

    public void InitFloor()
    {
        for (int x = 0; x < gridManager.GetWidthMax(); x++)
        {
            for (int z = 0; z < gridManager.GetHeightMax(); z++)
            {
                if ((z < gridManager.GetHeightMin() && x < gridManager.GetHeightMax()) ||
                    (z >= gridManager.GetHeightMin() && x < gridManager.GetWidthMin() && z < gridManager.GetHeightMax()))
                {
                    GameObject floor = GameObject.Instantiate(floorPrefab, gridManager.GetWorldPosition(x, z), Quaternion.identity);
                    floor.transform.SetParent(floorContainer);
                    floor.transform.localPosition = new Vector3(floor.transform.localPosition.x, 0f, floor.transform.localPosition.z);
                }
               
            }
        }
    }

    public void InitDefaultCounters()
    {
        List<GridObjectData>[,] gridObjectDataList = 
            JsonConvert.DeserializeObject<List<GridObjectData>[,]>
            (GameDefine.GridArrayDataInit,gameManager.DataHandler.Settings);
        gridManager.AddGridObjectData(gridObjectDataList);
    }
}
