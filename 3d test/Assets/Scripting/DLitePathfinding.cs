using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class DLitePathfinding : MonoBehaviour
{
    public GridGenerator grid;
    
    private DStarPriorityQueue priorityQueue = new DStarPriorityQueue();
    private Dictionary<Vector2Int, float> gValues = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, float> rhsValues = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, Node> nodeCache = new Dictionary<Vector2Int, Node>();
    private object lockObj = new object();
    private HashSet<Vector2Int> pendingUpdates = new HashSet<Vector2Int>();
    private Vector2Int lastStart;
    private Vector2Int lastGoal;
    private float km = 0;

    struct DStarKey : System.IComparable<DStarKey>
    {
        public float k1;
        public float k2;
        
        public int CompareTo(DStarKey other)
        {
            int k1Comp = k1.CompareTo(other.k1);
            return k1Comp != 0 ? k1Comp : k2.CompareTo(other.k2);
        }
    }

    void Awake()
    {
        if (grid == null) grid = GetComponent<GridGenerator>();
    }

    void Update()
    {
        ProcessPendingUpdates();
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (!ValidateInputs(start, goal)) return new List<Vector2Int>();

        lock (lockObj)
        {
            if (lastGoal != goal) Initialize(goal);
            if (lastStart != start) km += Heuristic(lastStart, start);
            
            lastStart = start;
            ComputeShortestPath();

            return gValues[start] < float.PositiveInfinity 
                ? ReconstructPath(start, goal) 
                : new List<Vector2Int>();
        }
    }

    public void MarkCellChanged(Vector2Int cell)
    {
        lock (lockObj)
        {
            if (grid.IsInBounds(cell))
                pendingUpdates.Add(cell);
        }
    }

    private void ProcessPendingUpdates()
    {
        lock (lockObj)
        {
            if (pendingUpdates.Count == 0) return;
            
            foreach (var cell in pendingUpdates)
                UpdateVertex(cell);
                
            pendingUpdates.Clear();
        }
    }

    private void Initialize(Vector2Int goal)
    {
        priorityQueue.Clear();
        gValues.Clear();
        rhsValues.Clear();
        nodeCache.Clear();
        km = 0;
        lastGoal = goal;

        foreach (var pos in grid.Nodes.Keys)
        {
            gValues[pos] = float.PositiveInfinity;
            rhsValues[pos] = float.PositiveInfinity;
            nodeCache[pos] = grid.GetNode(pos);
        }

        rhsValues[goal] = 0;
        priorityQueue.Enqueue(goal, CalculateKey(goal));
    }

    private void ComputeShortestPath()
    {
        while (priorityQueue.Count > 0 && 
              (priorityQueue.PeekKey().CompareTo(CalculateKey(lastStart)) < 0 || 
               rhsValues[lastStart] != gValues[lastStart]))
        {
            var u = priorityQueue.Dequeue();
            var uKey = CalculateKey(u);

            if (uKey.CompareTo(CalculateKey(u)) < 0)
            {
                priorityQueue.Enqueue(u, CalculateKey(u));
            }
            else if (gValues[u] > rhsValues[u])
            {
                gValues[u] = rhsValues[u];
                foreach (var s in GetPredecessors(u))
                    UpdateVertex(s);
            }
            else
            {
                gValues[u] = float.PositiveInfinity;
                UpdateVertex(u);
                foreach (var s in GetPredecessors(u))
                    UpdateVertex(s);
            }
        }
    }

    private void UpdateVertex(Vector2Int u)
    {
        if (u != lastGoal)
        {
            rhsValues[u] = GetNeighbors(u)
                .Select(s => GetCost(u, s) + gValues[s])
                .DefaultIfEmpty(float.PositiveInfinity)
                .Min();
        }

        priorityQueue.Remove(u);
        if (gValues[u] != rhsValues[u])
            priorityQueue.Enqueue(u, CalculateKey(u));
    }

    private DStarKey CalculateKey(Vector2Int s)
    {
        return new DStarKey
        {
            k1 = Mathf.Min(gValues[s], rhsValues[s]) + Heuristic(s, lastStart) + km,
            k2 = Mathf.Min(gValues[s], rhsValues[s])
        };
    }

    private List<Vector2Int> ReconstructPath(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = start;

        // Special case: if already adjacent to goal
        if (GetNeighbors(current).Contains(goal))
        {
            path.Add(goal);
            return path;
        }

        while (current != goal)
        {
            path.Add(current);
            
            // Find best next node
            Vector2Int next = current;
            float minCost = float.PositiveInfinity;
            
            foreach (var neighbor in GetNeighbors(current))
            {
                // Check if neighbor is goal
                if (neighbor == goal)
                {
                    path.Add(goal);
                    return path;
                }

                float cost = GetCost(current, neighbor) + gValues[neighbor];
                if (cost < minCost)
                {
                    minCost = cost;
                    next = neighbor;
                }
            }

            if (next == current)
            {
                Debug.LogWarning("Path reconstruction stuck at: " + current);
                break;
            }

            current = next;
        }

        return path;
    }

    private float GetCost(Vector2Int a, Vector2Int b)
    {
        // Always allow moving into destination
        if (b == lastGoal) return Vector2Int.Distance(a, b);
        
        if (!nodeCache.TryGetValue(a, out var nodeA) || !nodeA.isWalkable ||
            !nodeCache.TryGetValue(b, out var nodeB) || !nodeB.isWalkable)
            return float.PositiveInfinity;
            
        return Vector2Int.Distance(a, b);
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }

    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        }.Where(n => grid.IsInBounds(n)).ToList();
    }

    private List<Vector2Int> GetPredecessors(Vector2Int pos)
    {
        return GetNeighbors(pos);
    }

    private bool ValidateInputs(Vector2Int start, Vector2Int goal)
    {
        return grid != null && grid.IsInBounds(start) && grid.IsInBounds(goal);
    }

    private class DStarPriorityQueue
    {
        private SortedDictionary<DStarKey, Queue<Vector2Int>> queueDict = new SortedDictionary<DStarKey, Queue<Vector2Int>>();
        private Dictionary<Vector2Int, DStarKey> keyDict = new Dictionary<Vector2Int, DStarKey>();

        public int Count { get; private set; }

        public void Enqueue(Vector2Int item, DStarKey key)
        {
            if (!queueDict.ContainsKey(key))
                queueDict[key] = new Queue<Vector2Int>();
                
            queueDict[key].Enqueue(item);
            keyDict[item] = key;
            Count++;
        }

        public Vector2Int Dequeue()
        {
            var first = queueDict.First();
            var item = first.Value.Dequeue();
            keyDict.Remove(item);
            Count--;
            
            if (first.Value.Count == 0)
                queueDict.Remove(first.Key);
                
            return item;
        }

        public DStarKey PeekKey()
        {
            return queueDict.First().Key;
        }

        public void Remove(Vector2Int item)
        {
            if (keyDict.TryGetValue(item, out var key))
            {
                var queue = queueDict[key];
                var newQueue = new Queue<Vector2Int>(queue.Where(x => x != item));
                
                if (newQueue.Count != queue.Count)
                {
                    queueDict[key] = newQueue;
                    keyDict.Remove(item);
                    Count--;
                    
                    if (newQueue.Count == 0)
                        queueDict.Remove(key);
                }
            }
        }

        public void Clear()
        {
            queueDict.Clear();
            keyDict.Clear();
            Count = 0;
        }
    }
}