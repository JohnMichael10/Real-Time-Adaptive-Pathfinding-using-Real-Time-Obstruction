using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class NavAgent : MonoBehaviour
{
    [Header("Pathfinding")]
    public MonoBehaviour pathfinder; // Can be AStarPathfinding, DLitePathfinding, or HybridPathfinding
    public Transform destination;
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float waypointAdvanceDistance = 0.25f;

    [Header("Height Settings")]
    public float baseHeight = 0.5f;
    public float heightOffset = 0.25f;

    [Header("Destination Settings")]
    public float destinationSnapDistance = 0.1f;
    public float destinationStopDistance = 0.5f;

    [Header("Movement Control")]
    public KeyCode movementKey = KeyCode.Space;
    public bool requireKeyPress = true;
    public bool startWithMovementEnabled = false;

    [Header("Debug")]
    public bool showPathGizmos = true;
    public Color pathColor = Color.cyan;
    public Color currentTargetColor = Color.green;

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
        }

        if (grid != null)
        {
            currentGridPosition = grid.WorldToGridPosition(transform.position);
            
            if (!grid.IsWalkable(currentGridPosition))
            {
                grid.ToggleObstruction(currentGridPosition, false);
            }
        }

        if ((movementEnabled || !requireKeyPress) && isMoving && !isSnappingToGrid && !hasReachedDestination)
        {
            UpdateMovement();
            UpdateRotation();
            CheckDestinationReached();
        }
    }

    void CheckDestinationReached()
    {
        if (destination == null) return;

        float distanceToDestination = Vector3.Distance(transform.position, destination.position);
        if (distanceToDestination <= destinationStopDistance)
        {
            SnapToDestination();
        }
    }

    void SnapToDestination()
    {
        hasReachedDestination = true;
        isMoving = false;
        
        Vector2Int destGridPos = grid.WorldToGridPosition(destination.position);
        Vector3 snappedPosition = grid.GridToWorldPosition(destGridPos);
        snappedPosition.y = GetTargetHeight();
        transform.position = snappedPosition;
        
        rb.velocity = Vector3.zero;
        path = null;
        pathIndex = 0;
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
            transform.position = currentTarget;
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

            if (hasReachedDestination && 
                Vector3.Distance(transform.position, destination.position) > destinationStopDistance)
            {
                hasReachedDestination = false;
            }

            if (hasReachedDestination) continue;

            Vector2Int startPos = grid.WorldToGridPosition(transform.position);
            Vector2Int targetPos = grid.WorldToGridPosition(destination.position);

            if (!grid.IsWalkable(targetPos))
            {
                isMoving = false;
                continue;
            }

            List<Vector2Int> newPath = GetPath(startPos, targetPos);
            
            if (newPath != null && newPath.Count > 0)
            {
                path = newPath;
                pathIndex = 0;
                isMoving = true;
            }
            else if (Vector2Int.Distance(startPos, targetPos) <= 1)
            {
                SnapToDestination();
            }
            else
            {
                isMoving = false;
            }
        }
    }

    private List<Vector2Int> GetPath(Vector2Int start, Vector2Int target)
    {
        if (pathfinder is AStarPathfinding aStar)
        {
            return aStar.FindPath(start, target);
        }
        else if (pathfinder is DLitePathfinding dLite)
        {
            return dLite.FindPath(start, target);
        }
        else if (pathfinder is HybridPathfinding hybrid)
        {
            return hybrid.FindPath(start, target);
        }
        
        Debug.LogError("No valid pathfinder component found!");
        return new List<Vector2Int>();
    }

    bool ValidateComponents()
    {
        if (destination == null) 
        {
            Debug.LogWarning("Destination is not set!");
            return false;
        }

        if (grid == null) 
        {
            grid = FindObjectOfType<GridGenerator>();
            if (grid == null)
            {
                Debug.LogError("No GridGenerator found in scene!");
                return false;
            }
        }

        if (pathfinder == null) 
        {
            pathfinder = FindObjectOfType<HybridPathfinding>() ?? 
                        (MonoBehaviour)FindObjectOfType<AStarPathfinding>() ?? 
                        (MonoBehaviour)FindObjectOfType<DLitePathfinding>();
            
            if (pathfinder == null)
            {
                Debug.LogError("No pathfinder component (A*, DLite, or Hybrid) found in scene!");
                return false;
            }
        }

        return true;
    }

    void OnDrawGizmosSelected()
    {
        if (!showPathGizmos || path == null || grid == null) return;

        Gizmos.color = pathColor;
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = grid.GridToWorldPosition(path[i]);
            Vector3 end = grid.GridToWorldPosition(path[i + 1]);
            start.y = end.y = GetTargetHeight() + 0.1f;
            Gizmos.DrawLine(start, end);
        }

        if (isMoving && pathIndex < path.Count)
        {
            Gizmos.color = currentTargetColor;
            Gizmos.DrawWireSphere(currentTarget, 0.3f);
        }

        if (destination != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(destination.position, destinationStopDistance);
        }
    }
}