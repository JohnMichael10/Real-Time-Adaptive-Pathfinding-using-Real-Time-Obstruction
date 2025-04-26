using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq; // Added for LINQ methods

public class HybridPathfinding : MonoBehaviour
{
    public GridGenerator grid;
    
    [Header("Debug Settings")]
    public bool logPathSwitching = true;
    public Color aStarLogColor = Color.cyan;
    public Color dLiteLogColor = Color.yellow;

    [Header("Mode Switching")]
    [Tooltip("Delay before allowing DLite to be used")]
    public float dliteActivationDelay = 5f;

    // D* Lite structures
    private PriorityQueue<Vector2Int, DStarKey> priorityQueue = new PriorityQueue<Vector2Int, DStarKey>();
    private Dictionary<Vector2Int, float> gValues = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, float> rhsValues = new Dictionary<Vector2Int, float>();
    private Dictionary<Vector2Int, Node> nodeCache = new Dictionary<Vector2Int, Node>();
    private HashSet<Vector2Int> changedCells = new HashSet<Vector2Int>();
    private Vector2Int lastGoal;
    private float km = 0;

    // Hybrid control
    private bool useDLite = false;
    private bool dliteAllowed = false;
    private HashSet<Vector2Int> lastObstructionState = new HashSet<Vector2Int>();

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

    void Start()
    {
        if (grid != null)
        {
            lastObstructionState = new HashSet<Vector2Int>(grid.GetObstructionPositions());
        }
        StartCoroutine(EnableDLiteAfterDelay());
    }

    private IEnumerator EnableDLiteAfterDelay()
    {
        yield return new WaitForSeconds(dliteActivationDelay);
        dliteAllowed = true;
        
        if (logPathSwitching)
        {
            Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + 
                     ">DLite now available (after " + dliteActivationDelay + " seconds)</color>");
        }
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (dliteAllowed)
        {
            CheckForObstructionChanges();
        }

        if (logPathSwitching)
        {
            Debug.Log(useDLite 
                ? "<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + ">Using DLite</color>"
                : "<color=#" + ColorUtility.ToHtmlStringRGBA(aStarLogColor) + ">Using A*</color>");
        }

        return useDLite ? DLiteFindPath(start, goal) : AStarFindPath(start, goal);
    }

    public void MarkCellChanged(Vector2Int cell)
    {
        if (grid.IsInBounds(cell))
        {
            changedCells.Add(cell);
        }
    }

    private void CheckForObstructionChanges()
    {
        if (grid == null) return;

        var currentObstructions = new HashSet<Vector2Int>(grid.GetObstructionPositions());
        bool newObstructions = false;

        foreach (var pos in currentObstructions)
        {
            if (!lastObstructionState.Contains(pos))
            {
                newObstructions = true;
                MarkCellChanged(pos);
            }
        }

        if (newObstructions)
        {
            useDLite = true;
            if (logPathSwitching)
            {
                Debug.Log("<color=#" + ColorUtility.ToHtmlStringRGBA(dLiteLogColor) + 
                         ">New obstructions detected - switching to DLite</color>");
            }
        }

        lastObstructionState = currentObstructions;
    }

    private List<Vector2Int> AStarFindPath(Vector2Int start, Vector2Int goal)
    {
        Node startNode = grid.GetNode(start);
        Node goalNode = grid.GetNode(goal);
        grid.ResetAllPathfindingData();

        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedList = new HashSet<Vector2Int>();

        openList.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = Vector2Int.Distance(start, goal);

        while (openList.Count > 0)
        {
            openList.Sort((a, b) => a.FCost.CompareTo(b.FCost));
            Node currentNode = openList[0];

            if (currentNode.position == goal)
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode.position);

            foreach (Vector2Int neighborPos in GetNeighbors(currentNode.position))
            {
                Node neighborNode = grid.GetNode(neighborPos);
                if (neighborNode == null || !neighborNode.isWalkable || closedList.Contains(neighborPos))
                    continue;

                float newCost = currentNode.gCost + Vector2Int.Distance(currentNode.position, neighborPos);
                if (newCost < neighborNode.gCost || !openList.Contains(neighborNode))
                {
                    neighborNode.gCost = newCost;
                    neighborNode.hCost = Vector2Int.Distance(neighborPos, goal);
                    neighborNode.parent = currentNode;

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        return new List<Vector2Int>();
    }

    private List<Vector2Int> DLiteFindPath(Vector2Int start, Vector2Int goal)
    {
        if (lastGoal != goal)
        {
            InitializeDLite(goal);
            lastGoal = goal;
        }

        km += Heuristic(start, lastGoal);
        ProcessChangedCells();
        ComputeShortestPath(start);

        if (gValues.ContainsKey(start) && gValues[start] < float.PositiveInfinity)
        {
            return ReconstructDLitePath(start, goal);
        }

        return new List<Vector2Int>();
    }

    private void InitializeDLite(Vector2Int goal)
    {
        priorityQueue.Clear();
        gValues.Clear();
        rhsValues.Clear();
        nodeCache.Clear();
        changedCells.Clear();
        km = 0;

        foreach (var pos in grid.Nodes.Keys)
        {
            gValues[pos] = float.PositiveInfinity;
            rhsValues[pos] = float.PositiveInfinity;
            nodeCache[pos] = grid.GetNode(pos);
        }

        rhsValues[goal] = 0;
        priorityQueue.Enqueue(goal, CalculateKey(goal));
    }

    private void ProcessChangedCells()
    {
        foreach (var cell in changedCells)
        {
            UpdateVertex(cell);
        }
        changedCells.Clear();
    }

    private void ComputeShortestPath(Vector2Int start)
    {
        while (priorityQueue.Count > 0 && 
              (priorityQueue.Peek().CompareTo(CalculateKey(start)) < 0 || 
               rhsValues[start] != gValues[start]))
        {
            Vector2Int u = priorityQueue.Dequeue();
            DStarKey kOld = CalculateKey(u);
            DStarKey kNew = CalculateKey(u);

            if (kOld.CompareTo(kNew) < 0)
            {
                priorityQueue.Enqueue(u, kNew);
            }
            else if (gValues[u] > rhsValues[u])
            {
                gValues[u] = rhsValues[u];
                foreach (var s in GetPredecessors(u))
                {
                    UpdateVertex(s);
                }
            }
            else
            {
                gValues[u] = float.PositiveInfinity;
                UpdateVertex(u);
                foreach (var s in GetPredecessors(u))
                {
                    UpdateVertex(s);
                }
            }
        }
    }

    private void UpdateVertex(Vector2Int u)
    {
        if (u != lastGoal)
        {
            rhsValues[u] = GetMinSuccessorCost(u);
        }

        priorityQueue.Remove(u);
        if (gValues[u] != rhsValues[u])
        {
            priorityQueue.Enqueue(u, CalculateKey(u));
        }
    }

    private float GetMinSuccessorCost(Vector2Int u)
    {
        float minCost = float.PositiveInfinity;
        foreach (var neighbor in GetNeighbors(u))
        {
            float cost = GetCost(u, neighbor) + gValues[neighbor];
            if (cost < minCost)
            {
                minCost = cost;
            }
        }
        return minCost;
    }

    private DStarKey CalculateKey(Vector2Int s)
    {
        float h = Heuristic(s, lastGoal);
        return new DStarKey
        {
            k1 = Mathf.Min(gValues[s], rhsValues[s]) + h + km,
            k2 = Mathf.Min(gValues[s], rhsValues[s])
        };
    }

    private List<Vector2Int> ReconstructDLitePath(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int current = start;

        while (current != goal)
        {
            path.Add(current);
            Vector2Int next = current;
            float minCost = float.PositiveInfinity;

            foreach (var neighbor in GetNeighbors(current))
            {
                float cost = GetCost(current, neighbor) + gValues[neighbor];
                if (cost < minCost)
                {
                    minCost = cost;
                    next = neighbor;
                }
            }

            if (next == current)
            {
                break; // No path found
            }

            current = next;
        }

        path.Add(goal);
        return path;
    }

    private float GetCost(Vector2Int a, Vector2Int b)
    {
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
        }.FindAll(n => grid.IsInBounds(n));
    }

    private List<Vector2Int> GetPredecessors(Vector2Int pos)
    {
        return GetNeighbors(pos);
    }

    private List<Vector2Int> RetracePath(Node start, Node end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node current = end;
        while (current != start)
        {
            path.Add(current.position);
            current = current.parent;
        }
        path.Reverse();
        return path;
    }
}

// Priority Queue implementation
public class PriorityQueue<TElement, TPriority> where TPriority : System.IComparable<TPriority>
{
    private SortedDictionary<TPriority, Queue<TElement>> dictionary = new SortedDictionary<TPriority, Queue<TElement>>();
    private Dictionary<TElement, TPriority> elementToPriority = new Dictionary<TElement, TPriority>();

    public int Count { get; private set; }

    public void Enqueue(TElement item, TPriority priority)
    {
        if (!dictionary.ContainsKey(priority))
        {
            dictionary[priority] = new Queue<TElement>();
        }
        dictionary[priority].Enqueue(item);
        elementToPriority[item] = priority;
        Count++;
    }

    public TElement Dequeue()
    {
        var first = dictionary.First();
        var item = first.Value.Dequeue();
        elementToPriority.Remove(item);
        Count--;
        
        if (first.Value.Count == 0)
        {
            dictionary.Remove(first.Key);
        }
        return item;
    }

    public TPriority Peek()
    {
        return dictionary.First().Key;
    }

    public void Remove(TElement item)
    {
        if (elementToPriority.TryGetValue(item, out var priority))
        {
            var queue = dictionary[priority];
            var newQueue = new Queue<TElement>(queue.ToArray().Where(x => !x.Equals(item)));
            
            if (newQueue.Count != queue.Count)
            {
                dictionary[priority] = newQueue;
                elementToPriority.Remove(item);
                Count -= (queue.Count - newQueue.Count);
                
                if (newQueue.Count == 0)
                {
                    dictionary.Remove(priority);
                }
            }
        }
    }

    public void Clear()
    {
        dictionary.Clear();
        elementToPriority.Clear();
        Count = 0;
    }
}