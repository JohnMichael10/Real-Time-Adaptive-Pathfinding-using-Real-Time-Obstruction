using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NavAgent : MonoBehaviour
{
    public AStarPathfinding pathfinder;
    public Transform destination;
    public float moveSpeed = 100f;

    private List<Vector2Int> path;
    private int pathIndex;

    void Start()
    {
        StartCoroutine(MoveToDestination());
    }

    IEnumerator MoveToDestination()
    {
        Vector2Int start = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.z));
        Vector2Int goal = new Vector2Int(Mathf.RoundToInt(destination.position.x), Mathf.RoundToInt(destination.position.z));

        path = pathfinder.FindPath(start, goal);
        pathIndex = 0;

        while (pathIndex < path.Count)
        {
            Vector3 targetPos = new Vector3(path[pathIndex].x, transform.position.y, path[pathIndex].y);
            while (Vector3.Distance(transform.position, targetPos) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                yield return null;
            }
            pathIndex++;
        }
    }
}
