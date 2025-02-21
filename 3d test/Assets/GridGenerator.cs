using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public GameObject tilePrefab;
    public GameObject obstructionPrefab; // Prefab for the visual obstruction
    public int gridWidth = 25;
    public int gridHeight = 25;
    public float tileSize = 4;
    public float obstructionProbability = 0.2f; // 20% chance for an obstruction

    private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    private Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();

    void Start()
    {
        GenerateGrid();
        GenerateRandomObstructions();
    }

    void GenerateGrid()
    {
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 spawnPos = new Vector3(x * tileSize - offsetX, 0, z * tileSize - offsetZ);
                GameObject tile = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);

                Vector2Int gridPos = new Vector2Int(x, z);
                nodes[gridPos] = new Node(gridPos);
                tiles[gridPos] = tile;
            }
        }
    }

    void GenerateRandomObstructions()
    {
        foreach (var node in nodes.Values)
        {
            if (Random.value < obstructionProbability)
            {
                // Set node as obstructed
                ToggleObstruction(node.position, true);
                // Instantiate a visual obstruction at this tile's location
                Vector3 obstructionPos = tiles[node.position].transform.position;
                Instantiate(obstructionPrefab, obstructionPos, Quaternion.identity, transform);
            }
        }
    }

    public void ToggleObstruction(Vector2Int pos, bool isObstructed)
    {
        if (nodes.ContainsKey(pos))
        {
            nodes[pos].isWalkable = !isObstructed;

            // Optional: Change the tile's color to indicate the state change.
            Renderer renderer = tiles[pos].GetComponent<Renderer>();
            renderer.material.color = isObstructed ? Color.red : Color.white;
        }
    }

    public Node GetNode(Vector2Int pos)
    {
        if (nodes.ContainsKey(pos))
            return nodes[pos];
        return null;
    }
    
    public bool IsWalkable(Vector2Int pos)
    {
        if (nodes.ContainsKey(pos))
            return nodes[pos].isWalkable;
        return false; // or true if you consider off-grid as walkable
    }
}
