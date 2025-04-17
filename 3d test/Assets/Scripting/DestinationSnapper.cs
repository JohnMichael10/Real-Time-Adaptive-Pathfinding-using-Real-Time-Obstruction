using UnityEngine;
using System.Collections;

public class DestinationSnapper : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Duration of the snap animation in seconds")]
    public float snapDuration = 0.5f;
    
    private GridGenerator grid;
    private bool isSnapping = false;

    void Awake()
    {
        grid = FindObjectOfType<GridGenerator>();
        StartCoroutine(SnapToGrid());
    }

    public IEnumerator SnapToGrid()
    {
        isSnapping = true;
        
        if (grid == null) yield break;
        
        Vector2Int gridPos = grid.WorldToGridPosition(transform.position);
        Vector3 targetPosition = grid.GridToWorldPosition(gridPos);
        targetPosition.y = 0; // Ground level

        float elapsedTime = 0f;
        Vector3 startPosition = transform.position;

        while (elapsedTime < snapDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / snapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        isSnapping = false;
    }

    public bool IsSnapping() => isSnapping;

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (grid == null) return;
        
        Gizmos.color = Color.magenta;
        Vector2Int gridPos = grid.WorldToGridPosition(transform.position);
        Vector3 center = grid.GridToWorldPosition(gridPos);
        Gizmos.DrawWireCube(center, Vector3.one * grid.tileSize * 0.8f);
    }
    #endif
}