using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class DOTPathfinding : PersistentSingleton<DOTPathfinding>
{
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private void Start()
    {
        FindPathJob findPathJob = new FindPathJob
        {
            startPosition = new int2(0, 0),
            endPosition = new int2(3, 1)
        };
        findPathJob.Schedule().Complete();

    }

    [BurstCompile]
    private struct FindPathJob : IJob
    {
        public int2 startPosition;
        public int2 endPosition;
        public void Execute()
        {
            int2 gridSize = new int2(4, 4);

            NativeArray<PathNodeStruct> pathNodeArray = new NativeArray<PathNodeStruct>(gridSize.x * gridSize.y, Allocator.Temp);

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    PathNodeStruct pathNode = new PathNodeStruct();
                    pathNode.x = x;
                    pathNode.y = y;
                    pathNode.index = CalculateIndex(x, y, gridSize.x);

                    pathNode.gCost = int.MaxValue;
                    pathNode.hCost = CalculateDistanceCost(new int2(x, y), endPosition);
                    pathNode.CalculateFCost();

                    pathNode.isWalkable = true;
                    pathNode.cameFromNodeIndex = -1;

                    pathNodeArray[pathNode.index] = pathNode;

                }
            }

            //{
            //    PathNodeStruct walkablePathNode = pathNodeArray[CalculateIndex(1, 0, gridSize.x)];
            //    walkablePathNode.SetIsWalkable(false);
            //    pathNodeArray[CalculateIndex(1, 0, gridSize.x)] = walkablePathNode;

            //    walkablePathNode = pathNodeArray[CalculateIndex(1, 1, gridSize.x)];
            //    walkablePathNode.SetIsWalkable(false);
            //    pathNodeArray[CalculateIndex(1, 1, gridSize.x)] = walkablePathNode;

            //    walkablePathNode = pathNodeArray[CalculateIndex(1, 2, gridSize.x)];
            //    walkablePathNode.SetIsWalkable(false);
            //    pathNodeArray[CalculateIndex(1, 2, gridSize.x)] = walkablePathNode;
            //}

            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(8, Allocator.Temp);
            neighbourOffsetArray[0] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[2] = new int2(0, +1); // Up
            neighbourOffsetArray[3] = new int2(0, -1); // Down
            neighbourOffsetArray[4] = new int2(-1, -1); // Left Down
            neighbourOffsetArray[5] = new int2(-1, +1); // Left Up
            neighbourOffsetArray[6] = new int2(+1, -1); // Right Down
            neighbourOffsetArray[7] = new int2(+1, +1); // Right Up


            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

            PathNodeStruct startNode = pathNodeArray[CalculateIndex(startPosition.x, startPosition.y, gridSize.x)];
            startNode.gCost = 0;
            startNode.CalculateFCost();
            pathNodeArray[startNode.index] = startNode;

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closeList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);
            while (openList.Length > 0)
            {
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, pathNodeArray);
                PathNodeStruct currentNode = pathNodeArray[currentNodeIndex];

                if (currentNodeIndex == endNodeIndex)
                {
                    break;
                }
                for (int i = 0; i < openList.Length; i++)
                {
                    if (openList[i] == currentNodeIndex)
                    {
                        openList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                closeList.Add(currentNodeIndex);

                for (int i = 0; i < neighbourOffsetArray.Length; i++)
                {
                    int2 neighbourOffset = neighbourOffsetArray[i];
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffset.x, currentNode.y + neighbourOffset.y);

                    if (!IsPositionInsideGrid(neighbourPosition, gridSize))
                    {
                        continue;
                    }

                    int neighbourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, gridSize.x);

                    if (closeList.Contains(neighbourNodeIndex))
                    {
                        continue;
                    }

                    PathNodeStruct neighbourNode = pathNodeArray[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable)
                    {
                        continue;
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tenrativeGCost = currentNode.gCost + CalculateDistanceCost(currentNodePosition, neighbourPosition);
                    if (tenrativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tenrativeGCost;
                        neighbourNode.CalculateFCost();
                        pathNodeArray[neighbourNodeIndex] = neighbourNode;

                        if (!openList.Contains(neighbourNode.index))
                        {
                            openList.Add(neighbourNode.index);
                        }
                    }
                }
            }

            PathNodeStruct endNode = pathNodeArray[endNodeIndex];
            if (endNode.cameFromNodeIndex == -1)
            {
                // No path found
                Debug.Log("No path found");
            }
            else
            {
                // Path found
                NativeList<int2> path = CalculatePath(pathNodeArray, endNode);
                foreach (int2 pathNode in path)
                {
                    Debug.Log($"({pathNode.x},{pathNode.y})");
                }
                path.Dispose();
            }


            pathNodeArray.Dispose();
            openList.Dispose();
            neighbourOffsetArray.Dispose();
            closeList.Dispose();
        }
        private NativeList<int2> CalculatePath(NativeArray<PathNodeStruct> pathNodeArray, PathNodeStruct endNode)
        {
            if (endNode.cameFromNodeIndex == -1)
            {
                // Couldn't find a path!
                return new NativeList<int2>(Allocator.Temp);
            }
            else
            {
                // Found a path
                NativeList<int2> path = new NativeList<int2>(Allocator.Temp);
                path.Add(new int2(endNode.x, endNode.y));

                PathNodeStruct currentNode = endNode;
                while (currentNode.cameFromNodeIndex != -1)
                {
                    PathNodeStruct cameFromNode = pathNodeArray[currentNode.cameFromNodeIndex];
                    path.Add(new int2(cameFromNode.x, cameFromNode.y));
                    currentNode = cameFromNode;
                }

                return path;
            }
        }
        private bool IsPositionInsideGrid(int2 position, int2 gridSize)
        {
            return position.x >= 0 && position.x < gridSize.x &&
                position.y >= 0 && position.y < gridSize.y;
        }
        private int GetLowestCostFNodeIndex(NativeList<int> openList, NativeArray<PathNodeStruct> pathNodeArray)
        {
            PathNodeStruct lowestCostPathNode = pathNodeArray[openList[0]];
            for (int i = 0; i < openList.Length; i++)
            {
                PathNodeStruct testPathNode = pathNodeArray[openList[i]];
                if (testPathNode.fCost < lowestCostPathNode.fCost)
                {
                    lowestCostPathNode = testPathNode;
                }
            }
            return lowestCostPathNode.index;
        }
        private int CalculateDistanceCost(int2 aPosition, int2 bPosition)
        {
            int xDistance = math.abs(aPosition.x - bPosition.x);
            int yDistance = math.abs(aPosition.y - bPosition.y);
            int remaining = math.abs(xDistance - yDistance);
            return MOVE_DIAGONAL_COST * math.min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
        }

        private int CalculateIndex(int x, int y, int gridWidth)
        {
            return x + y * gridWidth;
        }
        private struct PathNodeStruct
        {
            public int x;
            public int y;

            public int index;

            public int gCost;
            public int hCost;
            public int fCost;

            public bool isWalkable;
            public int cameFromNodeIndex;

            public void CalculateFCost()
            {
                fCost = gCost + hCost;
            }
            public void SetIsWalkable(bool isWalkable)
            {
                this.isWalkable = isWalkable;
            }
        }
    }
    
    
}