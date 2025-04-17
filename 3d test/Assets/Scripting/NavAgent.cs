using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class NavAgent : MonoBehaviour
{
    [Header("Pathfinding")]
    public AStarPathfinding pathfinder;
    public Transform destination;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float waypointAdvanceDistance = 0.25f;

    [Header("Height Settings")]
    public float baseHeight = 0.5f;
    public float heightOffset = 0.25f;

    [Header("Movement Control")]
    public KeyCode movementKey = KeyCode.Space;
    public bool requireKeyPress = true;
    public bool startWithMovementEnabled = false;

    private List<Vector2Int> path;
    private int pathIndex;
    private Rigidbody rb;
    private GridGenerator grid;
    private Vector3 currentTarget;
    private bool isMoving = false;
    private Vector3 movementVelocity;
    private float rotationVelocity;
    private bool isSnappingToGrid = false;
    private bool hasReachedDestination = false;
    private Vector2Int currentGridPosition;
    
    [SerializeField] 
    private bool movementEnabled;
    public bool IsMovementEnabled => movementEnabled;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        grid = FindObjectOfType<GridGenerator>();
        movementEnabled = startWithMovementEnabled;
        StartCoroutine(SnapToGridSmoothly(transform, true));
    }

    void Start()
    {
        if (!ValidateComponents()) return;
        StartCoroutine(PathfindingUpdate());
    }

    void Update()
    {
        if (Input.GetKeyDown(movementKey))
        {
            movementEnabled = !movementEnabled;
            Debug.Log($"Movement {(movementEnabled ? "enabled" : "disabled")}");
        }

        if (grid != null)
        {
            currentGridPosition = grid.WorldToGridPosition(transform.position);
            
            if (!grid.IsWalkable(currentGridPosition))
            {
                grid.ToggleObstruction(currentGridPosition, false);
                Debug.LogWarning("Cleared obstruction at agent position");
            }
        }

        if ((movementEnabled || !requireKeyPress) && isMoving && !isSnappingToGrid)
        {
            UpdateMovement();
            UpdateRotation();
            CheckDestinationReached();
        }
    }

    void CheckDestinationReached()
    {
        if (path == null || path.Count == 0 || pathIndex >= path.Count)
        {
            float distanceToDestination = Vector3.Distance(transform.position, destination.position);
            if (distanceToDestination <= waypointAdvanceDistance && !hasReachedDestination)
            {
                hasReachedDestination = true;
                Debug.Log("Pathfinding Complete!");
                isMoving = false;
            }
        }
    }

    IEnumerator SnapToGridSmoothly(Transform targetTransform, bool isAgent)
    {
        isSnappingToGrid = true;
        
        if (grid == null) yield break;
        
        Vector2Int gridPos = grid.WorldToGridPosition(targetTransform.position);
        Vector3 targetPosition = grid.GridToWorldPosition(gridPos);
        targetPosition.y = isAgent ? GetTargetHeight() : 0f;

        float snapDuration = 0.5f;
        float elapsedTime = 0f;
        Vector3 startPosition = targetTransform.position;

        while (elapsedTime < snapDuration)
        {
            targetTransform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / snapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        targetTransform.position = targetPosition;
        isSnappingToGrid = false;
        
        if (isAgent) InitializePosition();
    }

    void InitializePosition()
    {
        Vector3 pos = transform.position;
        pos.y = GetTargetHeight();
        transform.position = pos;
    }

    float GetTargetHeight()
    {
        return baseHeight + heightOffset;
    }

    void UpdateMovement()
    {
        if (path == null || pathIndex >= path.Count) return;

        currentTarget = grid.GridToWorldPosition(path[pathIndex]);
        currentTarget.y = GetTargetHeight();

        transform.position = Vector3.SmoothDamp(
            transform.position,
            currentTarget,
            ref movementVelocity,
            0.2f,
            moveSpeed
        );

        if (Vector3.Distance(transform.position, currentTarget) <= waypointAdvanceDistance)
        {
            pathIndex++;
        }
    }

    void UpdateRotation()
    {
        Vector3 direction = currentTarget - transform.position;
        direction.y = 0;

        if (direction.magnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                0.2f,
                rotationSpeed
            );
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    IEnumerator PathfindingUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            
            if (!ValidateComponents() || isSnappingToGrid)
            {
                isMoving = false;
                continue;
            }

            hasReachedDestination = false;

            Vector2Int startPos = grid.WorldToGridPosition(transform.position);
            Vector2Int targetPos = grid.WorldToGridPosition(destination.position);

            if (!grid.IsWalkable(targetPos))
            {
                Debug.LogWarning("Target position is obstructed!");
                isMoving = false;
                continue;
            }

            List<Vector2Int> newPath = pathfinder.FindPath(startPos, targetPos);
            
            if (newPath != null && newPath.Count > 0)
            {
                path = newPath;
                pathIndex = 0;
                isMoving = true;
            }
            else if (Vector2Int.Distance(startPos, targetPos) <= 1)
            {
                Debug.Log("Pathfinding Complete! (Already at destination)");
                isMoving = false;
            }
            else
            {
                Debug.LogWarning("Pathfinding failed - no valid path found!");
                isMoving = false;
            }
        }
    }

    bool ValidateComponents()
    {
        if (destination == null) return false;
        if (grid == null) grid = FindObjectOfType<GridGenerator>();
        if (pathfinder == null) pathfinder = FindObjectOfType<AStarPathfinding>();
        return grid != null && pathfinder != null;
    }

    void OnDrawGizmosSelected()
    {
        if (path == null || grid == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = grid.GridToWorldPosition(path[i]);
            Vector3 end = grid.GridToWorldPosition(path[i + 1]);
            start.y = end.y = GetTargetHeight() + 0.1f;
            Gizmos.DrawLine(start, end);
        }

        if (isMoving && pathIndex < path.Count)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(currentTarget, 0.3f);
        }
    }
}