using UnityEngine;
using UnityEngine.Profiling;

public class PathfindingPerformanceTester : MonoBehaviour
{
    private NavAgent agent;
    private bool isMeasuring = false;
    private float startTime;
    private long startMemory;

    void Start()
    {
        agent = GetComponent<NavAgent>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (agent != null && agent.destination != null)
            {
                StartMeasurement();
            }
        }

        if (isMeasuring && agent != null && agent.IsMovementEnabled && HasReachedDestination())
        {
            EndMeasurement();
        }
    }

    void StartMeasurement()
    {
        Debug.Log("<color=cyan>Starting pathfinding measurement...</color>");
        startTime = Time.realtimeSinceStartup;
        startMemory = Profiler.GetTotalAllocatedMemoryLong();
        isMeasuring = true;
    }

    void EndMeasurement()
    {
        float endTime = Time.realtimeSinceStartup;
        long endMemory = Profiler.GetTotalAllocatedMemoryLong();

        float timeElapsed = endTime - startTime;
        float memoryAllocatedMB = endMemory / (1024f * 1024f); // Total memory allocated in MB

        Debug.Log("<b><color=lime>Pathfinding completed!</color></b>");
        Debug.Log($"<color=yellow>Time Elapsed: {timeElapsed:F4} seconds</color>");
        Debug.Log($"<color=orange>Total Memory Allocated: {memoryAllocatedMB:F4} MB</color>"); // Updated log message

        isMeasuring = false;
    }

    bool HasReachedDestination()
    {
        if (agent.destination == null) return false;
        float distance = Vector3.Distance(transform.position, agent.destination.position);
        return distance <= agent.destinationStopDistance + 0.05f; // Allow some margin
    }
}