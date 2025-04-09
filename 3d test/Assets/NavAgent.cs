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
    [Tooltip("Minimum distance before advancing to next waypoint")]
    public float waypointAdvanceDistance = 0.25f;

    [Header("Height Settings")]
    [Tooltip("Base height above ground plane")]
    public float baseHeight = 0.5f;
    [Tooltip("Additional height offset to prevent clipping")]
    public float heightOffset = 0.25f;

    private List<Vector2Int> path;
    private int pathIndex;
    private Rigidbody rb;
    private GridGenerator grid;
    private Vector3 currentTarget;
    private bool isMoving = false;
    private Vector3 movementVelocity;
    private float rotationVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        grid = FindObjectOfType<GridGenerator>();
        InitializePosition();
    }

    void Start()
    {
        if (!ValidateComponents()) return;
        StartCoroutine(PathfindingUpdate());
    }

    void Update()
    {
        if (isMoving)
        {
            UpdateMovement();
            UpdateRotation();
        }
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

        // Smooth damp movement for vibration-free motion
        transform.position = Vector3.SmoothDamp(
            transform.position,
            currentTarget,
            ref movementVelocity,
            0.2f, // Smoothing time
            moveSpeed
        );

        // Check if we should advance to next waypoint
        if (Vector3.Distance(transform.position, currentTarget) <= waypointAdvanceDistance)
        {
            pathIndex++;
        }
    }

    void UpdateRotation()
    {
        Vector3 direction = currentTarget - transform.position;
        direction.y = 0; // Ignore vertical component

        if (direction.magnitude > 0.01f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                0.2f, // Rotation smoothing time
                rotationSpeed
            );
            transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }

    IEnumerator PathfindingUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Reduced update frequency
            
            if (!ValidateComponents())
            {
                isMoving = false;
                continue;
            }

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
            else
            {
                Debug.LogWarning("Pathfinding failed!");
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