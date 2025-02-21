using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstructionPlacer : MonoBehaviour
{
    public GridGenerator gridGenerator;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left click to toggle obstruction
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 hitPosition = hit.point;
                Vector2Int gridPos = new Vector2Int(
                    Mathf.RoundToInt((hitPosition.x + (gridGenerator.gridWidth - 1) * gridGenerator.tileSize / 2) / gridGenerator.tileSize),
                    Mathf.RoundToInt((hitPosition.z + (gridGenerator.gridHeight - 1) * gridGenerator.tileSize / 2) / gridGenerator.tileSize)
                );

                gridGenerator.ToggleObstruction(gridPos, true); // Mark the tile as obstructed
            }
        }
    }
}
