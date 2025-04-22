using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class HybridPathfinding : MonoBehaviour
{
    public GridGenerator grid;
    
    [Header("Debug Settings")]
    public bool logPathSwitching = true;
    public Color aStarLogColor = Color.cyan;
    public Color dLiteLogColor = Color.yellow;

    [Header("Mode Switching")]
    [Tooltip("Delay before allowing DLite to be used")]
    public float dliteActivationDelay = 5f;

    private List<Node> openList = new List<Node>();
    private HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> lastObstructionState = new HashSet<Vector2Int>();
    private bool useDLite = false;
    private bool dliteAllowed = false;

    void Start()
    {
        // Initialize obstruction tracking
        if (grid != null)
        {
            lastObstructionState = new HashSet<Vector2Int>(grid.GetObstructionPositions());
        }

        // Start delay timer
        StartCoroutine(EnableDLiteAfterDelay());
    }

    private IEnumerator EnableDLiteAfterDelay()
    {
        yield return new WaitForSeconds(dliteActivationDelay);
        dliteAllowed = true;
        
        if (logPathSwitching)
        {
            Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + 
                     ">DLite now available (after " + dliteActivationDelay + " seconds)</color>");
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        // Check for obstruction changes (only if DLite is allowed)
        if (dliteAllowed)
        {
            CheckForObstructionChanges();
        }

        if (logPathSwitching)
        {
            Debug.Log(useDLite 
                ? "<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + ">Using DLite</color>"
                : "<color=#" + ColorUtility.ToHtmlStringRGBA(aStarLogColor) + ">Using A*</color>");
        }

        return useDLite ? DLiteFindPath(start, goal) : AStarFindPath(start, goal);
    }

    private void CheckForObstructionChanges()
    {
        if (grid == null) return;

        // Get current obstructions
        var currentObstructions = new HashSet<Vector2Int>(grid.GetObstructionPositions());

        // Check for new obstructions
        bool newObstructions = false;
        foreach (var pos in currentObstructions)
        {
            if (!lastObstructionState.Contains(pos))
            {
                newObstructions = true;
                break;
            }
        }

        // Switch to DLite if new obstructions found
        if (newObstructions)
        {
            useDLite = true;
            if (logPathSwitching)
            {
                Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + 
                         ">New obstructions detected - switching to DLite</color>");
            }
        }

        // Update last known state
        lastObstructionState = currentObstructions;
    }

    private List<Vector2Int> AStarFindPath(Vector2Int start, Vector2Int goal)
    {
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

        return new List<Vector2Int>();
    }

    private List<Vector2Int> DLiteFindPath(Vector2Int start, Vector2Int goal)
    {
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

        return new List<Vector2Int>();
    }

    private List<Vector2Int> RetracePath(Node start, Node end)
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

    private List<Vector2Int> GetNeighbors(Vector2Int nodePos)
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
}