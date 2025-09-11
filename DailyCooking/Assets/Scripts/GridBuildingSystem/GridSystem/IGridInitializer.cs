using System.Collections.Generic;
using UnityEngine;

public interface IGridInitializer
{
    void InitRoad();
    void InitPillar();
    void InitWallAndFloor();
    void InitDefaultCounters();
    void PlaceWall(int x, int z, int gridWidth, int gridHeight);
    List<GridWall> GridWallList { get; }
}
