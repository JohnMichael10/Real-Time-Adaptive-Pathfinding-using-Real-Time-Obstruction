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

        Node startNode = new Node(start);
        Node goalNode = new Node(goal);

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
                if (closedList.Contains(neighborPos)) continue;

                Node neighborNode = new Node(neighborPos)
                {
                    gCost = currentNode.gCost + 1,
                    hCost = Vector2Int.Distance(neighborPos, goal),
                    parent = currentNode
                };

                if (!openList.Exists(n => n.position == neighborPos && n.FCost <= neighborNode.FCost))
                {
                    openList.Add(neighborNode);
                }
            }
        }
        return new List<Vector2Int>(); // No path found
    }

    List<Vector2Int> GetNeighbors(Vector2Int nodePos)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>
        {
            nodePos + Vector2Int.up, nodePos + Vector2Int.down,
            nodePos + Vector2Int.left, nodePos + Vector2Int.right
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
