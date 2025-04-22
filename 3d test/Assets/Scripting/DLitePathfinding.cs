using System.Collections.Generic;
using UnityEngine;

public class DLitePathfinding : MonoBehaviour
{
    public GridGenerator grid;

    private List<Node> openList = new List<Node>();
    private HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

    void Awake()
    {
        if (grid == null) grid = GetComponent<GridGenerator>();
        if (grid == null) Debug.LogError("GridGenerator reference missing!");
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        // Early validation checks
        if (!ValidateInputs(start, goal))
        {
            return new List<Vector2Int>(); // Return empty path if inputs are invalid
        }

        Node startNode = grid.GetNode(start);
        Node goalNode = grid.GetNode(goal);

        // Reset pathfinding data
        grid.ResetAllPathfindingData();
        openList.Clear();
        closedList.Clear();

        openList.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = Vector2Int.Distance(start, goal);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.FCost.CompareTo(b.FCost));
            Node currentNode = openList[0];

            if (currentNode.position == goal)
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.position);

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode.position))
            {
                Node neighborNode = grid.GetNode(neighborPos);
                if (neighborNode == null || !neighborNode.isWalkable || closedList.Contains(neighborPos))
                    continue;

                float newCost = currentNode.gCost + Vector2Int.Distance(currentNode.position, neighborPos);
                if (newCost < neighborNode.gCost || !openList.Contains(neighborNode))
                {
                    neighborNode.gCost = newCost;
                    neighborNode.hCost = Vector2Int.Distance(neighborPos, goal);
                    neighborNode.parent = currentNode;

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        Debug.LogWarning("Pathfinding failed - no valid path found!");
        return new List<Vector2Int>();
    }

    List<Vector2Int> GetNeighbors(Vector2Int nodePos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            nodePos + Vector2Int.up,
            nodePos + Vector2Int.down,
            nodePos + Vector2Int.left,
            nodePos + Vector2Int.right
        };
        return neighbors.FindAll(n => grid.IsInBounds(n));
    }

    List<Vector2Int> RetracePath(Node start, Node end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node current = end;
        while (current != start)
        {
            path.Add(current.position);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }

    private bool ValidateInputs(Vector2Int start, Vector2Int goal)
    {
        return grid != null && grid.IsInBounds(start) && grid.IsInBounds(goal) &&
               grid.IsWalkable(start) && grid.IsWalkable(goal);
    }
}