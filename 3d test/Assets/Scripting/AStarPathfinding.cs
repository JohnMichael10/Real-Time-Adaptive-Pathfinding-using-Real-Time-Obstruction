using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding : MonoBehaviour
{
    public GridGenerator grid;

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (grid == null || !IsValidNode(start) || !IsValidNode(goal))
            return new List<Vector2Int>(); // Return empty path if invalid

        Node startNode = grid.GetNode(start);
        Node goalNode = grid.GetNode(goal);
        grid.ResetAllPathfindingData();

        List<Node> openList = new List<Node> { startNode };
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();
        startNode.gCost = 0;
        startNode.hCost = Vector2Int.Distance(start, goal);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);
            if (currentNode.position == goal) return RetracePath(startNode, currentNode);

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
                        openList.Add(neighborNode);
                }
            }
        }

        Debug.LogWarning("Pathfinding failed - no valid path found!");
        return new List<Vector2Int>();
    }

    private Node GetLowestFCostNode(List<Node> openList)
    {
        openList.Sort((a, b) => a.FCost.CompareTo(b.FCost));
        return openList[0];
    }

    private List<Vector2Int> GetNeighbors(Vector2Int nodePos)
    {
        return new List<Vector2Int>
        {
            nodePos + Vector2Int.up,
            nodePos + Vector2Int.down,
            nodePos + Vector2Int.left,
            nodePos + Vector2Int.right
        }.FindAll(n => grid.IsInBounds(n));
    }

    private List<Vector2Int> RetracePath(Node start, Node end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        for (Node current = end; current != start; current = current.parent)
            path.Add(current.position);
        path.Reverse();
        return path;
    }

    private bool IsValidNode(Vector2Int pos)
    {
        Node node = grid.GetNode(pos);
        return node != null && node.isWalkable;
    }
}