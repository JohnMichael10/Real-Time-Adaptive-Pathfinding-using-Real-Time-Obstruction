using UnityEngine;

public class Node
{
    public Vector2Int position;
    public float gCost, hCost;
    public Node parent;

    public float FCost => gCost + hCost;

    public Node(Vector2Int pos)
    {
        position = pos;
    }
}
