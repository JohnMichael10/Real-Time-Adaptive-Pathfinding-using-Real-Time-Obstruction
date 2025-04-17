using UnityEngine;

public class ObstructionPlacer : MonoBehaviour
{
    [Header("References")]
    public GridGenerator gridGenerator;
    
    [Header("Visual Effects")]
    public ParticleSystem placementEffect;
    public Texture2D placeCursor;
    public Texture2D removeCursor;
    
    private Camera mainCamera;
    private Vector2 cursorHotspot = new Vector2(16, 16);

    void Start()
    {
        mainCamera = Camera.main;
        if (placementEffect != null)
        {
            placementEffect.Stop();
        }
        Cursor.SetCursor(placeCursor, cursorHotspot, CursorMode.Auto);
    }

    void Update()
    {
        HandleObstructionPlacement();
    }

    void HandleObstructionPlacement()
    {
        if (gridGenerator == null || mainCamera == null) return;

        if (Input.GetMouseButtonDown(0)) // Left click to place obstruction
        {
            TryModifyObstruction(true);
        }
        else if (Input.GetMouseButtonDown(1)) // Right click to remove obstruction
        {
            TryModifyObstruction(false);
        }
    }

    void TryModifyObstruction(bool placeObstruction)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridPos = gridGenerator.WorldToGridPosition(hit.point);
            
            // Prevent modifying destination tile
            if (gridGenerator.destination != null && 
                gridPos == gridGenerator.WorldToGridPosition(gridGenerator.destination.transform.position))
            {
                PlayPlacementEffects(hit.point, false);
                return;
            }

            Node node = gridGenerator.GetNode(gridPos);
            
            if (node != null)
            {
                if (placeObstruction && node.isWalkable)
                {
                    gridGenerator.ToggleObstruction(gridPos, true);
                    PlayPlacementEffects(hit.point, true);
                }
                else if (!placeObstruction && !node.isWalkable)
                {
                    gridGenerator.ToggleObstruction(gridPos, false);
                    PlayPlacementEffects(hit.point, true);
                }
            }
        }
    }

    void PlayPlacementEffects(Vector3 position, bool success)
    {
        if (placementEffect != null)
        {
            placementEffect.transform.position = position;
            var main = placementEffect.main;
            main.startColor = success ? Color.red : Color.yellow;
            placementEffect.Play();
        }
    }
}