using UnityEngine;

public class ObstructionPlacer : MonoBehaviour
{
    public GridGenerator gridGenerator;
    public bool placeObstructions = true;
    public KeyCode toggleModeKey = KeyCode.Space;
    public ParticleSystem placementEffect;
    public AudioClip placementSound;
    public Texture2D placeCursor, removeCursor;
    
    private Camera mainCamera;
    private Vector2 cursorHotspot = new Vector2(16, 16);

    void Start()
    {
        mainCamera = Camera.main;
        if (placementEffect != null) placementEffect.Stop();
        UpdateCursor();
    }

    void Update()
    {
        HandleModeToggle();
        HandleObstructionPlacement();
    }

    void HandleModeToggle()
    {
        if (Input.GetKeyDown(toggleModeKey))
        {
            placeObstructions = !placeObstructions;
            Debug.Log($"Obstruction mode: {(placeObstructions ? "Place" : "Remove")}");
            UpdateCursor();
        }
    }

    void HandleObstructionPlacement()
    {
        if (Input.GetMouseButtonDown(0) && gridGenerator != null && mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector2Int gridPos = gridGenerator.WorldToGridPosition(hit.point);
                Node node = gridGenerator.GetNode(gridPos);
                
                if (node != null)
                {
                    // Place obstruction only if in place mode and tile is walkable
                    if (placeObstructions && node.isWalkable)
                    {
                        gridGenerator.ToggleObstruction(gridPos, true);
                        PlayPlacementEffects(hit.point);
                    }
                    // Remove obstruction only if in remove mode and tile is blocked
                    else if (!placeObstructions && !node.isWalkable)
                    {
                        gridGenerator.ToggleObstruction(gridPos, false);
                        PlayPlacementEffects(hit.point);
                    }
                }
            }
        }
    }

    void UpdateCursor()
    {
        if (placeObstructions && placeCursor != null)
        {
            Cursor.SetCursor(placeCursor, cursorHotspot, CursorMode.Auto);
        }
        else if (!placeObstructions && removeCursor != null)
        {
            Cursor.SetCursor(removeCursor, cursorHotspot, CursorMode.Auto);
        }
    }

    void PlayPlacementEffects(Vector3 position)
    {
        if (placementEffect != null)
        {
            placementEffect.transform.position = position;
            placementEffect.Play();
        }

        if (placementSound != null)
        {
            AudioSource.PlayClipAtPoint(placementSound, position);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), 
            $"Obstruction Mode: {(placeObstructions ? "Place" : "Remove")}");
        GUI.Label(new Rect(10, 30, 200, 30), 
            $"Press {toggleModeKey} to toggle mode");
    }
}