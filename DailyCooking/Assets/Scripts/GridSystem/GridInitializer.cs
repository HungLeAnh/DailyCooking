using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class GridInitializer : IGridInitializer
{
    private IGridManager gridManager;
    private Transform roadContainer;
    private GameObject roadPrefab;
    private GameObject roadCornerPrefab;
    private Transform pillarContainer;
    private GameObject pillarPrefab;
    private Transform wallContainer;
    private GameObject wallPrefab;
    private Transform floorContainer;
    private GameObject floorPrefab;
    private List<GridWall> gridWallList = new List<GridWall>();

    public List<GridWall> GridWallList => gridWallList;

    public GridInitializer(IGridManager gridManager, Transform roadContainer, GameObject roadPrefab, GameObject roadCornerPrefab, Transform pillarContainer, GameObject pillarPrefab, Transform wallContainer, GameObject wallPrefab, Transform floorContainer, GameObject floorPrefab)
    {
        this.gridManager = gridManager;
        this.roadContainer = roadContainer;
        this.roadPrefab = roadPrefab;
        this.roadCornerPrefab = roadCornerPrefab;
        this.pillarContainer = pillarContainer;
        this.pillarPrefab = pillarPrefab;
        this.wallContainer = wallContainer;
        this.wallPrefab = wallPrefab;
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

    public void InitPillar()
    {
        if(gridManager.GetWidth() == 0 || gridManager.GetHeight() == 0)
            return;
        GameObject botLeftPillar = GameObject.Instantiate(pillarPrefab, gridManager.GetWorldPosition(0, 0), Quaternion.identity);
        botLeftPillar.transform.SetParent(roadContainer);
        botLeftPillar.transform.rotation = Quaternion.Euler(0, 0, 0);
        gridWallList.Add(botLeftPillar.GetComponent<GridWall>());

        GameObject botRightPillar = GameObject.Instantiate(pillarPrefab, gridManager.GetWorldPosition(0,gridManager.GetHeight() ), Quaternion.identity);
        botRightPillar.transform.SetParent(roadContainer);  
        botRightPillar.transform.rotation = Quaternion.Euler(0, 90, 0);
        gridWallList.Add(botRightPillar.GetComponent<GridWall>());

        GameObject topLeftPillar = GameObject.Instantiate(pillarPrefab, gridManager.GetWorldPosition(gridManager.GetWidth(), 0), Quaternion.identity);
        topLeftPillar.transform.SetParent(roadContainer);
        topLeftPillar.transform.rotation = Quaternion.Euler(0, 270, 0);
        gridWallList.Add(topLeftPillar.GetComponent<GridWall>());

        GameObject topRightPillar = GameObject.Instantiate(pillarPrefab, gridManager.GetWorldPosition(gridManager.GetWidth(),gridManager.GetHeight()), Quaternion.identity);
        topRightPillar.transform.SetParent(roadContainer);
        topRightPillar.transform.rotation = Quaternion.Euler(0, 180, 0);
        gridWallList.Add(topRightPillar.GetComponent<GridWall>());

    }

    public void InitWallAndFloor()
    {
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            for (int z = 0; z < gridManager.GetHeight(); z++)
            {
                GameObject floor = GameObject.Instantiate(floorPrefab, gridManager.GetWorldPosition(x, z), Quaternion.identity);
                floor.transform.SetParent(floorContainer);
                floor.transform.localPosition = new Vector3(floor.transform.localPosition.x, 0f, floor.transform.localPosition.z);

                if (x == 0 || z == 0 || x == gridManager.GetWidth() - 1 || z == gridManager.GetHeight() - 1)
                {
                    PlaceWall(x, z, gridManager.GetWidth(), gridManager.GetHeight());
                }
            }
        }
    }

    public void PlaceWall(int x, int z, int gridWidth, int gridHeight)
    {
        GameObject wall = GameObject.Instantiate(wallPrefab, gridManager.GetWorldPosition(x, z) +
            new Vector3(gridManager.GetCellSize() / 2, 0, gridManager.GetCellSize() / 2), Quaternion.identity);
        wall.transform.SetParent(wallContainer);
        gridWallList.Add(wall.GetComponent<GridWall>());

        if (x == 0) // Left border (facing right)
        {
            wall.transform.localPosition += new Vector3(-0.25f, 0, 0);
            wall.transform.rotation = Quaternion.Euler(0, 270, 0);
        }
        else if (x == gridWidth - 1) // Right border (facing left)
        {
            wall.transform.localPosition -= new Vector3(-0.25f, 0, 0);
            wall.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        if (z == 0) // Bottom border (facing down)
        {
            wall.transform.localPosition -= new Vector3(0, 0, 0.25f);
            wall.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (z == gridHeight - 1) // Top border (facing up)
        {
            wall.transform.localPosition += new Vector3(0, 0, 0.25f);
            wall.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        if (x == 0) // Bottom corner
        {
            if (z == 0 || z == gridHeight - 1)
            {
                GameObject blcwall = GameObject.Instantiate(wallPrefab, gridManager.GetWorldPosition(x, z) +
                                    new Vector3(gridManager.GetCellSize() / 2, 0, gridManager.GetCellSize() / 2), Quaternion.identity);
                blcwall.transform.SetParent(wallContainer);
                blcwall.transform.localPosition -= new Vector3(0.25f, 0, 0);
                blcwall.transform.rotation = Quaternion.Euler(0, 270, 0);
                gridWallList.Add(blcwall.GetComponent<GridWall>());

                wall.transform.localPosition += new Vector3(0.25f, 0, 0);
            }
        }
        else if (x == gridWidth - 1) // Top corner
        {
            if (z == 0 || z == gridHeight - 1)
            {
                GameObject brcwall = GameObject.Instantiate(wallPrefab, gridManager.GetWorldPosition(x, z) +
                                    new Vector3(gridManager.GetCellSize() / 2, 0, gridManager.GetCellSize() / 2), Quaternion.identity);
                brcwall.transform.SetParent(wallContainer);
                brcwall.transform.localPosition += new Vector3(0.25f, 0, 0);
                brcwall.transform.rotation = Quaternion.Euler(0, 90, 0);
                gridWallList.Add(brcwall.GetComponent<GridWall>());

                wall.transform.localPosition += new Vector3(-0.25f, 0, 0);
            }
        }
    }

    public void InitDefaultCounters()
    {
        List<GridObjectData> gridObjectDataList = JsonConvert.DeserializeObject<List<GridObjectData>>(GameDefine.GridArrayDataInit,GameManager.Instance.DataHandler.Settings);
        foreach (GridObjectData gridObject in gridObjectDataList)
        {
            if (GameManager.Instance.GameData.GridData.GridArrayData.Contains(gridObject))
                continue;
            GameManager.Instance.GameData.GridData.GridArrayData.Add(gridObject);
        }
        gridManager.AddGridObjectData(gridObjectDataList);
    }
}
