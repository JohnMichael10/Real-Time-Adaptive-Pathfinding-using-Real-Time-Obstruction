using UnityEngine;
using System.Collections;
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
    public float tileSize = 4f;
    
    [Header("Obstacle Settings")]
    [Range(0, 1)] public float obstructionProbability = 0.2f;
    public Color walkableColor = Color.white;
    public Color obstructedColor = Color.red;
    public Color dynamicModeColor = Color.yellow;

    [Header("Protected Positions")]
    public NavAgent navAgent;
    public GameObject destination;
    public List<Vector2Int> manualProtectedPositions = new List<Vector2Int>();

    [Header("Visualization")]
    public bool showGridInEditor = true;
    public Color editorGridColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);
    public bool showProtectedTiles = true;
    public Color protectedTileColor = Color.blue;
    public Color destinationColor = Color.magenta;

    [Header("Dynamic Obstructions")]
    public float dynamicObstructionInterval = 2f;
    public bool isInDynamicMode = false;
    public KeyCode toggleDynamicModeKey = KeyCode.G;

    // Core data structures
    private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
    private Dictionary<Vector2Int, GameObject> tiles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> obstructions = new Dictionary<Vector2Int, GameObject>();
    private HashSet<Vector2Int> protectedPositions = new HashSet<Vector2Int>();
    private Vector2Int lastAgentGridPos;
    private Vector2Int lastDestinationGridPos;
    private Coroutine dynamicObstructionRoutine;
    private List<GameObject> previewTiles = new List<GameObject>();

    public Dictionary<Vector2Int, Node> Nodes => nodes;
    public bool IsInDynamicMode => isInDynamicMode;

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
            GenerateGrid();
            ProtectCriticalPositions();
            GenerateRandomObstructions();
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            UpdateAgentProtection();
            UpdateDestinationProtection();

            if (Input.GetKeyDown(toggleDynamicModeKey))
            {
                ToggleDynamicMode(!isInDynamicMode);
            }
        }
    }

    #region Grid Management
    public void GenerateGrid()
    {
        ClearExistingGrid();
        
        float offsetX = (gridWidth - 1) * tileSize / 2f;
        float offsetZ = (gridHeight - 1) * tileSize / 2f;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                Vector3 worldPos = new Vector3(
                    x * tileSize - offsetX,
                    0,
                    z * tileSize - offsetZ
                );

                CreateTile(gridPos, worldPos);
                nodes[gridPos] = new Node(gridPos);
            }
        }
    }

    private void CreateTile(Vector2Int gridPos, Vector3 worldPos)
    {
        GameObject tile = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
        tile.name = $"Tile_{gridPos.x}_{gridPos.y}";
        
        if (tile.GetComponent<Collider>() == null)
        {
            tile.AddComponent<BoxCollider>();
        }

        tiles[gridPos] = tile;
    }

    private void ClearExistingGrid()
    {
        foreach (var tile in tiles.Values) if (tile != null) Destroy(tile);
        foreach (var obs in obstructions.Values) if (obs != null) Destroy(obs);
        
        nodes.Clear();
        tiles.Clear();
        obstructions.Clear();
        protectedPositions.Clear();
    }
    #endregion

    #region Obstruction Management
    public void GenerateRandomObstructions()
    {
        ClearAllObstructions();
        ProtectCriticalPositions();

        foreach (var node in nodes.Values)
        {
            if (!IsPositionProtected(node.position) && Random.value < obstructionProbability)
            {
                ToggleObstruction(node.position, true);
            }
        }
    }

    public void ToggleObstruction(Vector2Int pos, bool isObstructed)
    {
        if (IsPositionProtected(pos)) return;

        if (nodes.TryGetValue(pos, out Node node))
        {
            node.isWalkable = !isObstructed;
            UpdateTileVisual(pos, isObstructed);

            if (isObstructed && !obstructions.ContainsKey(pos))
            {
                CreateObstruction(pos);
            }
            else if (!isObstructed && obstructions.TryGetValue(pos, out GameObject obs))
            {
                Destroy(obs);
                obstructions.Remove(pos);
            }
        }
    }

    public void ClearObstruction(Vector2Int pos)
    {
        if (obstructions.ContainsKey(pos))
        {
            Destroy(obstructions[pos]);
            obstructions.Remove(pos);
            nodes[pos].isWalkable = true;
            UpdateTileVisual(pos, false);
        }
    }

    private void CreateObstruction(Vector2Int pos)
    {
        Vector3 obsPos = tiles[pos].transform.position;
        obsPos.y += obstructionPrefab.transform.localScale.y / 2;
        GameObject obstruction = Instantiate(obstructionPrefab, obsPos, Quaternion.identity, transform);
        obstructions[pos] = obstruction;
    }

    public void ToggleDynamicMode(bool enable)
    {
        if (enable == isInDynamicMode) return;
        
        isInDynamicMode = enable;
        
        if (enable)
        {
            dynamicObstructionRoutine = StartCoroutine(DynamicObstructionRoutine());
        }
        else if (dynamicObstructionRoutine != null)
        {
            StopCoroutine(dynamicObstructionRoutine);
        }
        
        UpdateAllTileColorsForDynamicMode(enable);
    }

    private IEnumerator DynamicObstructionRoutine()
    {
        while (isInDynamicMode)
        {
            yield return new WaitForSeconds(dynamicObstructionInterval);
            GenerateRandomObstructions();
        }
    }

    private void ClearAllObstructions()
    {
        foreach (var pos in new List<Vector2Int>(obstructions.Keys))
        {
            if (!IsPositionProtected(pos)) ClearObstruction(pos);
        }
    }
    #endregion

    #region Position Protection
    private void ProtectCriticalPositions()
    {
        protectedPositions.Clear();

        // Manual protected positions
        foreach (var pos in manualProtectedPositions)
        {
            protectedPositions.Add(pos);
            nodes[pos].isWalkable = true;
            UpdateTileVisual(pos, false);
        }

        // Agent position
        if (navAgent != null)
        {
            lastAgentGridPos = WorldToGridPosition(navAgent.transform.position);
            protectedPositions.Add(lastAgentGridPos);
            nodes[lastAgentGridPos].isWalkable = true;
            UpdateTileVisual(lastAgentGridPos, false);
        }

        // Destination position
        if (destination != null)
        {
            lastDestinationGridPos = WorldToGridPosition(destination.transform.position);
            protectedPositions.Add(lastDestinationGridPos);
            nodes[lastDestinationGridPos].isWalkable = true;
            UpdateTileVisual(lastDestinationGridPos, false);
        }
    }

    private void UpdateAgentProtection()
    {
        if (navAgent == null) return;

        Vector2Int currentPos = WorldToGridPosition(navAgent.transform.position);
        if (currentPos != lastAgentGridPos)
        {
            // Clear old protection
            if (protectedPositions.Contains(lastAgentGridPos))
            {
                protectedPositions.Remove(lastAgentGridPos);
                UpdateTileVisual(lastAgentGridPos, false);
            }

            // Add new protection
            protectedPositions.Add(currentPos);
            nodes[currentPos].isWalkable = true;
            UpdateTileVisual(currentPos, false);
            lastAgentGridPos = currentPos;
        }
    }

    private void UpdateDestinationProtection()
    {
        if (destination == null) return;

        Vector2Int currentPos = WorldToGridPosition(destination.transform.position);
        if (currentPos != lastDestinationGridPos)
        {
            // Clear old protection
            if (protectedPositions.Contains(lastDestinationGridPos))
            {
                protectedPositions.Remove(lastDestinationGridPos);
                UpdateTileVisual(lastDestinationGridPos, false);
            }

            // Add new protection
            protectedPositions.Add(currentPos);
            nodes[currentPos].isWalkable = true;
            UpdateTileVisual(currentPos, false);
            lastDestinationGridPos = currentPos;
        }
    }

    public bool IsPositionProtected(Vector2Int pos)
    {
        return protectedPositions.Contains(pos);
    }
    #endregion

    #region Tile Visualization
    private void UpdateTileVisual(Vector2Int pos, bool isObstructed)
    {
        if (!tiles.TryGetValue(pos, out GameObject tile)) return;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) return;

        if (IsDestination(pos))
        {
            renderer.material.color = destinationColor;
        }
        else if (protectedPositions.Contains(pos))
        {
            renderer.material.color = protectedTileColor;
        }
        else if (isInDynamicMode && !isObstructed)
        {
            renderer.material.color = dynamicModeColor;
        }
        else
        {
            renderer.material.color = isObstructed ? obstructedColor : walkableColor;
        }
    }

    private void UpdateAllTileColorsForDynamicMode(bool dynamicModeActive)
    {
        foreach (var kvp in tiles)
        {
            UpdateTileVisual(kvp.Key, !nodes[kvp.Key].isWalkable);
        }
    }

    private bool IsDestination(Vector2Int pos)
    {
        return destination != null && pos == WorldToGridPosition(destination.transform.position);
    }
    #endregion

    #region Coordinate Conversion
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
    #endregion

    #region Editor Utilities
    public void FindDestination()
    {
        if (destination == null)
        {
            var destObjs = GameObject.FindGameObjectsWithTag("Destination");
            if (destObjs.Length > 0) destination = destObjs[0];
        }
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
                Vector3 pos = new Vector3(
                    x * tileSize - offsetX,
                    0,
                    z * tileSize - offsetZ
                );
                
                GameObject tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
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
        foreach (var tile in previewTiles) if (tile != null) DestroyImmediate(tile);
        previewTiles.Clear();
    }
    #endregion

    #region Public API
    public Node GetNode(Vector2Int pos) => nodes.TryGetValue(pos, out Node node) ? node : null;
    public bool IsWalkable(Vector2Int pos) => nodes.TryGetValue(pos, out Node node) && node.isWalkable;
    public bool IsInBounds(Vector2Int pos) => 
        pos.x >= 0 && pos.x < gridWidth && 
        pos.y >= 0 && pos.y < gridHeight;

    public List<Vector2Int> GetObstructionPositions() => new List<Vector2Int>(obstructions.Keys);
    public void ResetAllPathfindingData()
    {
        foreach (Node node in nodes.Values) node.ResetPathfindingData();
    }
    #endregion

    #region Gizmos
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
    #endregion

    #region Editor
#if UNITY_EDITOR
    [CustomEditor(typeof(GridGenerator))]
    public class GridGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GridGenerator generator = (GridGenerator)target;
            
            if (GUILayout.Button("Find Destination")) generator.FindDestination();
            if (GUILayout.Button("Generate Preview")) generator.GenerateEditorPreview();
            if (GUILayout.Button("Clear Preview")) generator.ClearPreview();
            
            if (Application.isPlaying)
            {
                if (GUILayout.Button("Regenerate Grid")) generator.GenerateGrid();
                if (GUILayout.Button("Random Obstructions")) generator.GenerateRandomObstructions();
                if (GUILayout.Button(generator.IsInDynamicMode ? "Stop Dynamic Mode" : "Start Dynamic Mode"))
                {
                    generator.ToggleDynamicMode(!generator.IsInDynamicMode);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Grid operations only available in Play Mode", MessageType.Info);
            }
        }
    }
#endif
    #endregion
}