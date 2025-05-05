using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

public class TestScript
{
    private Pathfinding pathfinding;

    [SetUp]
    public void Setup()
    {
        // Initialize Pathfinding with a 10x10 grid
        pathfinding = new Pathfinding(10, 10);

        // Ensure all nodes are walkable
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                pathfinding.GetNode(x, y).SetIsWalkable(true);
            }
        }
    }

    [Test]
    public void FindPath_ReturnsValidPath_WhenPathExists()
    {
        // Arrange
        int startX = 0, startY = 0;
        int endX = 9, endY = 9;

        // Act
        List<PathNode> path = pathfinding.FindPath(startX, startY, endX, endY);

        // Assert
        Assert.IsNotNull(path, "Path should not be null.");
        Assert.IsTrue(path.Count > 0, "Path should contain nodes.");
        Assert.AreEqual(startX, Mathf.FloorToInt(path[0].x), "Path should start at the correct position.");
        Assert.AreEqual(endX, Mathf.FloorToInt(path[path.Count - 1].x), "Path should end at the correct position.");
    }

    [Test]
    public void FindPath_ReturnsNull_WhenPathDoesNotExist()
    {
        // Arrange
        int startX = 0, startY = 0;
        int endX = 9, endY = 9;

        // Set all nodes as unwalkable
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                pathfinding.GetNode(x, y).SetIsWalkable(false);
            }
        }

        // Act
        List<PathNode> path = pathfinding.FindPath(startX,startY,endX,endY);

        // Assert
        Assert.IsNull(path, "Path should be null when no path exists.");
    }

    [Test]
    public void FindPath_AvoidsUnwalkableNodes()
    {
        // Arrange
        int startX = 0, startY = 0;
        int endX = 9, endY = 9;

        // Set some nodes as unwalkable
        pathfinding.GetNode(2, 0).SetIsWalkable(false);
        pathfinding.GetNode(1, 1).SetIsWalkable(false);
        pathfinding.GetNode(0, 1).SetIsWalkable(false);

        // Act
        List<PathNode> path = pathfinding.FindPath(startX, startY, endX, endY);

        // Assert
        Assert.IsNotNull(path, "Path should not be null.");
        foreach (var position in path)
        {
            PathNode node = pathfinding.GetNode(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            Assert.IsTrue(node.isWalkable, $"Path should not pass through unwalkable nodes at ({node.x}, {node.y}).");
        }
    }
    [Test]
    [TestCase(10)]
    [TestCase(100)]
    [TestCase(200)]
    [TestCase(300)]
    [TestCase(400)]
    [TestCase(500)]

    public void FindPath_WithRandomStartAndEndPositions(int n)
    {
        Pathfinding looppathfinding = new Pathfinding(n,n);

        // Arrange
        int gridWidth = looppathfinding.GetGrid().GetWidth();
        int gridHeight = looppathfinding.GetGrid().GetHeight();

        Stopwatch stopwatch = new Stopwatch();
        System.Random random = new System.Random();
        int iterations = 100;
        List<long> times = new List<long>();
        

        // Act
        for (int i = 0; i < iterations; i++)
        {
            // Generate random start and end positions
            int startX = random.Next(0, gridWidth);
            int startY = random.Next(0, gridHeight);
            int endX = random.Next(0, gridWidth);
            int endY = random.Next(0, gridHeight);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    looppathfinding.GetNode(x, y).SetIsWalkable(true);
                }
            }

            // Add some random walls to simulate obstacles
            for (int j = 0; j < gridWidth / 4; j++)
            {
                int wallX = random.Next(0, gridWidth);
                int wallY = random.Next(0, gridHeight);
                looppathfinding.GetNode(wallX, wallY).SetIsWalkable(false);
            }
            stopwatch.Start();

            var path = looppathfinding.FindPath(startX, startY, endX, endY);

            stopwatch.Stop();
            times.Add(stopwatch.ElapsedMilliseconds);

        }

        // Calculate min, max, and mean times
        long minTime = times.Min();
        long maxTime = times.Max();
        double meanTime = times.Average();

        // Log results
        UnityEngine.Debug.Log($"Performance Test Results (100 Iterations):");
        UnityEngine.Debug.Log($"Min Time: {minTime} ms");
        UnityEngine.Debug.Log($"Max Time: {maxTime} ms");
        UnityEngine.Debug.Log($"Mean Time: {meanTime:F2} ms");
        
    }

}
