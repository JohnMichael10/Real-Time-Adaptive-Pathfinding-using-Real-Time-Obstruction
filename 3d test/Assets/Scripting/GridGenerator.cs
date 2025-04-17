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

    [Header("Protected Positions")]
    public NavAgent navAgent;
    [Tooltip("Leave empty to auto-find by 'Destination' tag")]
    public GameObject destination;
    public List<Vector2Int> manualProtectedPositions = new List<Vector2Int>();

    [Header("Visualization")]
    public bool showGridInEditor = true;
    public Color editorGridColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
    public bool showProtectedTiles = true;
    public Color protectedTileColor = Color.blue;
    public Color destinationColor = Color.magenta;

    [Header("Debug")]
    public bool showAgentProtectionDebug = false;

    private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    private Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> obstructions = new Dictionary<Vector2Int, GameObject>();
    private List<GameObject> previewTiles = new List<GameObject>();
    private HashSet<Vector2Int> protectedPositions = new HashSet<Vector2Int>();
    private Vector2Int lastAgentGridPos;
    private Vector2Int lastDestinationGridPos;

    public Dictionary<Vector2Int, Node> Nodes => nodes;

    void OnEnable()
    {
        FindDestination();
        ClearPreview();
        if (!Application.isPlaying && showGridInEditor)
        {
            GenerateEditorPreview();
        }
    }

    void Start()
    {
        FindDestination();
        if (Application.isPlaying)
        {
            ClearPreview();
            GenerateGrid();
            ProtectCriticalPositions();
            GenerateRandomObstructions();
            if (destination != null)
            {
                lastDestinationGridPos = WorldToGridPosition(destination.transform.position);
            }
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateAgentProtection();
            UpdateDestinationProtection();
        }
    }

    public void FindDestination()
    {
        if (destination == null)
        {
            GameObject[] destObjects = GameObject.FindGameObjectsWithTag("Destination");
            if (destObjects.Length > 0)
            {
                destination = destObjects[0];
                if (destObjects.Length > 1)
                {
                    Debug.LogWarning($"Multiple Destination objects found, using: {destination.name}");
                }
                Debug.Log($"Auto-assigned destination: {destination.name}");
            }
            else
            {
                Debug.LogError("No GameObject with 'Destination' tag found!");
            }
        }
    }

    void UpdateDestinationProtection()
    {
        if (destination == null)
        {
            FindDestination();
            if (destination == null) return;
        }

        Vector2Int currentDestPos = WorldToGridPosition(destination.transform.position);
        Node destNode = GetNode(currentDestPos);
        
        if (destNode == null) return;

        if (currentDestPos != lastDestinationGridPos)
        {
            if (protectedPositions.Contains(lastDestinationGridPos))
            {
                protectedPositions.Remove(lastDestinationGridPos);
                UpdateTileVisual(lastDestinationGridPos, false);
            }
            lastDestinationGridPos = currentDestPos;
        }

        if (obstructions.TryGetValue(currentDestPos, out GameObject obs))
        {
            Destroy(obs);
            obstructions.Remove(currentDestPos);
        }

        destNode.isWalkable = true;

        if (!protectedPositions.Contains(currentDestPos))
        {
            protectedPositions.Add(currentDestPos);
        }

        UpdateTileVisual(currentDestPos, false);
    }

    void UpdateAgentProtection()
    {
        if (navAgent == null) return;

        Vector2Int currentAgentPos = WorldToGridPosition(navAgent.transform.position);
        
        if (currentAgentPos != lastAgentGridPos)
        {
            if (protectedPositions.Contains(lastAgentGridPos))
            {
                protectedPositions.Remove(lastAgentGridPos);
                UpdateTileVisual(lastAgentGridPos, false);
            }

            if (!protectedPositions.Contains(currentAgentPos))
            {
                protectedPositions.Add(currentAgentPos);
                Node agentNode = GetNode(currentAgentPos);
                if (agentNode != null)
                {
                    agentNode.isWalkable = true;
                    UpdateTileVisual(currentAgentPos, false);
                }
            }

            lastAgentGridPos = currentAgentPos;
        }
    }

    bool IsDestination(Vector2Int pos)
    {
        return destination != null && pos == WorldToGridPosition(destination.transform.position);
    }

    void ProtectCriticalPositions()
    {
        protectedPositions.Clear();

        foreach (var pos in manualProtectedPositions)
        {
            protectedPositions.Add(pos);
            Node node = GetNode(pos);
            if (node != null)
            {
                node.isWalkable = true;
                UpdateTileVisual(pos, false);
            }
        }

        if (navAgent != null)
        {
            Vector2Int agentPos = WorldToGridPosition(navAgent.transform.position);
            protectedPositions.Add(agentPos);
            lastAgentGridPos = agentPos;
            
            Node agentNode = GetNode(agentPos);
            if (agentNode != null)
            {
                agentNode.isWalkable = true;
                UpdateTileVisual(agentPos, false);
            }
        }

        if (destination != null)
        {
            UpdateDestinationProtection();
        }
    }

    void GenerateGrid()
    {
        nodes.Clear();
        tiles.Clear();
        obstructions.Clear();

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

    public void GenerateRandomObstructions()
    {
        foreach (var obs in obstructions.Values)
        {
            if (obs != null) Destroy(obs);
        }
        obstructions.Clear();

        ProtectCriticalPositions();

        foreach (var node in nodes.Values)
        {
            if (IsPositionProtected(node.position))
            {
                node.isWalkable = true;
                UpdateTileVisual(node.position, false);
                continue;
            }

            if (Random.value < obstructionProbability)
            {
                ToggleObstruction(node.position, true);
            }
        }
    }

    public void ToggleObstruction(Vector2Int pos, bool isObstructed)
    {
        if (IsPositionProtected(pos))
        {
            if (isObstructed)
            {
                Debug.LogWarning($"Blocked obstruction at protected position: {pos}");
            }
            return;
        }

        if (nodes.TryGetValue(pos, out Node node))
        {
            node.isWalkable = !isObstructed;
            UpdateTileVisual(pos, isObstructed);

            if (isObstructed && !obstructions.ContainsKey(pos))
            {
                Vector3 obstructionPos = tiles[pos].transform.position;
                obstructionPos.y += obstructionPrefab.transform.localScale.y / 2;
                GameObject obstruction = Instantiate(obstructionPrefab, obstructionPos, Quaternion.identity, transform);
                obstructions[pos] = obstruction;
            }
            else if (!isObstructed && obstructions.TryGetValue(pos, out GameObject obs))
            {
                Destroy(obs);
                obstructions.Remove(pos);
            }
        }
    }

    void UpdateTileVisual(Vector2Int pos, bool isObstructed)
    {
        if (tiles.TryGetValue(pos, out GameObject tile))
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (IsDestination(pos))
                {
                    renderer.material.color = destinationColor;
                }
                else if (protectedPositions.Contains(pos))
                {
                    renderer.material.color = protectedTileColor;
                }
                else
                {
                    renderer.material.color = isObstructed ? obstructedColor : walkableColor;
                }
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

    public bool IsPositionProtected(Vector2Int pos)
    {
        return protectedPositions.Contains(pos) || IsDestination(pos);
    }

    public void ResetAllPathfindingData()
    {
        foreach (Node node in nodes.Values)
        {
            node.ResetPathfindingData();
        }
    }

    public void AddProtectedPosition(Vector2Int pos)
    {
        protectedPositions.Add(pos);
        if (nodes.TryGetValue(pos, out Node node))
        {
            node.isWalkable = true;
            UpdateTileVisual(pos, false);
        }
    }

    public void RemoveProtectedPosition(Vector2Int pos)
    {
        protectedPositions.Remove(pos);
    }

    public void GenerateEditorPreview()
    {
        ClearPreview();

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

        if (showProtectedTiles)
        {
            foreach (var pos in protectedPositions)
            {
                Vector3 center = GridToWorldPosition(pos);
                center.y = 0.1f;
                Gizmos.color = IsDestination(pos) ? destinationColor : protectedTileColor;
                Gizmos.DrawCube(center, new Vector3(tileSize * 0.9f, 0.1f, tileSize * 0.9f));
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
        
        if (GUILayout.Button("Find Destination"))
        {
            generator.FindDestination();
            EditorUtility.SetDirty(generator);
        }
        
        if (GUILayout.Button("Generate Preview"))
        {
            generator.GenerateEditorPreview();
        }
        
        if (GUILayout.Button("Clear Preview"))
        {
            generator.ClearPreview();
        }

        if (GUILayout.Button("Regenerate Obstructions"))
        {
            if (Application.isPlaying)
            {
                generator.GenerateRandomObstructions();
            }
            else
            {
                Debug.LogWarning("Can only regenerate obstructions in Play Mode");
            }
        }
    }
}
#endif