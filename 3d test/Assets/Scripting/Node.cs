using UnityEngine;

[System.Serializable]
public class Node
{
    public Vector2Int position;
    public bool isWalkable = true;
    
    // Pathfinding variables
    [System.NonSerialized] public float gCost; // Cost from start node
    [System.NonSerialized] public float hCost; // Heuristic cost to end node
    [System.NonSerialized] public Node parent; // For path reconstruction
    
    public float FCost => gCost + hCost;

    public Node(Vector2Int pos)
    {
        position = pos;
        isWalkable = true; // Nodes are walkable by default
    }

    // Reset pathfinding data for reuse
    public void ResetPathfindingData()
    {
        gCost = 0;
        hCost = 0;
        parent = null;
    }
}