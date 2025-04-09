using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding : MonoBehaviour
{
    public GridGenerator grid;
    public Transform startPoint, endPoint;

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        // Retrieve nodes from grid instead of creating new ones
        Node startNode = grid.GetNode(start);
        Node goalNode = grid.GetNode(goal);

        if (startNode == null || goalNode == null)
        {
            Debug.LogError("Start or goal node is null.");
            return new List<Vector2Int>();
        }

        openList.Add(startNode);

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
                // Check if neighbor exists in the grid and is walkable
                Node neighborNode = grid.GetNode(neighborPos);
                if (neighborNode == null || !neighborNode.isWalkable) continue;

                if (closedList.Contains(neighborPos)) continue;

                float newCost = currentNode.gCost + 1;
                if (newCost < neighborNode.gCost || !openList.Exists(n => n.position == neighborPos))
                {
                    neighborNode.gCost = newCost;
                    neighborNode.hCost = Vector2Int.Distance(neighborPos, goal);
                    neighborNode.parent = currentNode;

                    if (!openList.Exists(n => n.position == neighborPos))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }
        return new List<Vector2Int>(); // No path found
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
        return neighbors;
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
}