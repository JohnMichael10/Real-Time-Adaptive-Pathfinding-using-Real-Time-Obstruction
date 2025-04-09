using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject tilePrefab;
    public GameObject obstructionPrefab;
    public int gridWidth = 25;
    public int gridHeight = 25;
    public float tileSize = 4;
    
    [Header("Obstacle Settings")]
    [Range(0, 1)] public float obstructionProbability = 0.2f;
    public Color walkableColor = Color.white;
    public Color obstructedColor = Color.red;

    [Header("Editor Visualization")]
    public bool showGridInEditor = true;
    public Color editorGridColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

    private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    private Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> obstructions = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> previewTiles = new List<GameObject>();

    public Dictionary<Vector2Int, Node> Nodes => nodes;

    void OnEnable()
    {
        ClearPreview();
        if (!Application.isPlaying && showGridInEditor)
        {
            GenerateEditorPreview();
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            ClearPreview();
            GenerateGrid();
            GenerateRandomObstructions();
        }
    }

    void OnDisable()
    {
        ClearPreview();
    }

    public void GenerateEditorPreview()
    {
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 spawnPos = new Vector3(x * tileSize - offsetX, 0, z * tileSize - offsetZ);
                GameObject tile = Instantiate(tilePrefab, spawnPos, Quaternion.identity, transform);
                
                tile.name = $"PreviewTile_{x}_{z}";
                tile.hideFlags = HideFlags.DontSave;
                tile.tag = "EditorOnly";
                
                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = new Material(renderer.sharedMaterial);
                    renderer.sharedMaterial.color = editorGridColor;
                }

                previewTiles.Add(tile);
            }
        }
    }

    public void ClearPreview()
    {
        foreach (var tile in previewTiles)
        {
            if (tile != null)
            {
                DestroyImmediate(tile);
            }
        }
        previewTiles.Clear();
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
                tile.name = $"Tile_{x}_{z}";

                if (tile.GetComponent<Collider>() == null)
                {
                    tile.AddComponent<BoxCollider>();
                }

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
                ToggleObstruction(node.position, true);
            }
        }
    }

    public void ToggleObstruction(Vector2Int pos, bool isObstructed)
    {
        if (nodes.TryGetValue(pos, out Node node))
        {
            node.isWalkable = !isObstructed;
            UpdateTileVisual(pos, isObstructed);

            if (isObstructed && !obstructions.ContainsKey(pos))
            {
                Vector3 obstructionPos = tiles[pos].transform.position;
                obstructionPos.y += obstructionPrefab.transform.localScale.y / 2;
                GameObject obstruction = Instantiate(obstructionPrefab, obstructionPos, 
                    Quaternion.identity, transform);
                obstructions[pos] = obstruction;
            }
            else if (!isObstructed && obstructions.TryGetValue(pos, out GameObject obs))
            {
                Destroy(obs);
                obstructions.Remove(pos);
            }
        }
    }

    private void UpdateTileVisual(Vector2Int pos, bool isObstructed)
    {
        if (tiles.TryGetValue(pos, out GameObject tile))
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isObstructed ? obstructedColor : walkableColor;
            }
        }
    }

    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;
        return new Vector2Int(
            Mathf.RoundToInt((worldPos.x + offsetX) / tileSize),
            Mathf.RoundToInt((worldPos.z + offsetZ) / tileSize)
        );
    }

    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;
        return new Vector3(
            gridPos.x * tileSize - offsetX,
            0,
            gridPos.y * tileSize - offsetZ
        );
    }

    public Node GetNode(Vector2Int pos) => nodes.TryGetValue(pos, out Node node) ? node : null;
    
    public bool IsWalkable(Vector2Int pos) => nodes.TryGetValue(pos, out Node node) && node.isWalkable;

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = node.position + dir;
            if (nodes.TryGetValue(neighborPos, out Node neighbor))
            {
                neighbors.Add(neighbor);
            }
        }

        return neighbors;
    }

    public void ResetAllPathfindingData()
    {
        foreach (Node node in nodes.Values)
        {
            node.ResetPathfindingData();
        }
    }

    void OnDrawGizmos()
    {
        if (!showGridInEditor) return;

        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;

        Gizmos.color = editorGridColor;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 center = new Vector3(
                    x * tileSize - offsetX, 
                    0, 
                    z * tileSize - offsetZ
                );
                
                Gizmos.DrawWireCube(center, new Vector3(tileSize, 0.1f, tileSize));
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GridGenerator))]
public class GridGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GridGenerator generator = (GridGenerator)target;
        
        if (GUILayout.Button("Generate Preview"))
        {
            generator.GenerateEditorPreview();
        }
        
        if (GUILayout.Button("Clear Preview"))
        {
            generator.ClearPreview();
        }
    }
}
#endif