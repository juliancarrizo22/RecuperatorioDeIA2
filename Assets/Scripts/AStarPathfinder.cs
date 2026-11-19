
using System;
using System.Collections.Generic;
using UnityEngine;

public class PriorityQueue<TElement, TPriority>
    where TPriority : IComparable<TPriority>
{
    private readonly List<(TElement Element, TPriority Priority)> elements =
        new List<(TElement, TPriority)>();

    public int Count => elements.Count;

    public void Enqueue(TElement element, TPriority priority)
    {
        elements.Add((element, priority));

        int i = elements.Count - 1;

        while (i > 0)
        {
            int parent = (i - 1) / 2;

            if (elements[parent].Priority.CompareTo(
                elements[i].Priority) <= 0)
                break;

            var temp = elements[i];
            elements[i] = elements[parent];
            elements[parent] = temp;

            i = parent;
        }
    }

    public TElement Dequeue(out TPriority priority)
    {
        var bestItem = elements[0];

        priority = bestItem.Priority;

        int lastIndex = elements.Count - 1;

        elements[0] = elements[lastIndex];
        elements.RemoveAt(lastIndex);

        int i = 0;

        while (true)
        {
            int left = 2 * i + 1;
            int right = 2 * i + 2;
            int smallest = i;

            if (left < elements.Count &&
                elements[left].Priority.CompareTo(
                    elements[smallest].Priority) < 0)
            {
                smallest = left;
            }

            if (right < elements.Count &&
                elements[right].Priority.CompareTo(
                    elements[smallest].Priority) < 0)
            {
                smallest = right;
            }

            if (smallest == i)
                break;

            var temp = elements[i];
            elements[i] = elements[smallest];
            elements[smallest] = temp;

            i = smallest;
        }

        return bestItem.Element;
    }
}

public class AStarPathfinder : MonoBehaviour
{
    public static AStarPathfinder Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public List<GraphNode> FindPath(
        GraphNode start,
        GraphNode goal,
        Predicate<GraphNode> canTraverse = null,
        Func<GraphNode, float> extraCost = null,
        Func<GraphNode, GraphNode, bool> canTraverseEdge = null)
    {
        if (start == null || goal == null)
            return new List<GraphNode>();

        if (canTraverse != null)
        {
            if (!canTraverse(start) || !canTraverse(goal))
                return new List<GraphNode>();
        }

        PriorityQueue<GraphNode, float> openSet =
            new PriorityQueue<GraphNode, float>();

        Dictionary<GraphNode, float> openSetTracker =
            new Dictionary<GraphNode, float>();

        HashSet<GraphNode> closedSet =
            new HashSet<GraphNode>();

        Dictionary<GraphNode, GraphNode> cameFrom =
            new Dictionary<GraphNode, GraphNode>();

        Dictionary<GraphNode, float> gScore =
            new Dictionary<GraphNode, float>();

        gScore[start] = 0f;

        float initialPriority = Heuristic(start, goal);

        openSet.Enqueue(start, initialPriority);
        openSetTracker[start] = initialPriority;

        while (openSet.Count > 0)
        {
            GraphNode current = openSet.Dequeue(
                out float dequeuedPriority
            );

            if (!openSetTracker.TryGetValue(
                current, out float currentPriority))
            {
                continue;
            }

            if (dequeuedPriority.CompareTo(currentPriority) != 0)
            {
                continue;
            }

            openSetTracker.Remove(current);

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            closedSet.Add(current);

            foreach (GraphNode neighbor in current.neighbors)
            {
                if (neighbor == null)
                    continue;

                if (canTraverseEdge != null &&
                    !canTraverseEdge(current, neighbor))
                {
                    continue;
                }

                if (canTraverse != null &&
                    !canTraverse(neighbor))
                {
                    continue;
                }

                float stepCost = Vector3.Distance(
                    current.transform.position,
                    neighbor.transform.position
                );

                if (extraCost != null)
                    stepCost += extraCost(neighbor);

                float tentativeGScore =
                    gScore[current] + stepCost;

                if (gScore.TryGetValue(
                    neighbor, out float previousGScore) &&
                    tentativeGScore >= previousGScore)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;

                if (closedSet.Contains(neighbor))
                    closedSet.Remove(neighbor);

                float fScore =
                    tentativeGScore + Heuristic(neighbor, goal);

                openSet.Enqueue(neighbor, fScore);
                openSetTracker[neighbor] = fScore;
            }
        }

        return new List<GraphNode>();
    }

    public float CalculatePathCost(
        List<GraphNode> path,
        Func<GraphNode, float> extraCost = null)
    {
        if (path == null || path.Count == 0)
            return Mathf.Infinity;

        float total = 0f;

        for (int i = 0; i < path.Count - 1; i++)
        {
            total += Vector3.Distance(
                path[i].transform.position,
                path[i + 1].transform.position
            );

            if (extraCost != null)
                total += extraCost(path[i + 1]);
        }

        return total;
    }

    private float Heuristic(GraphNode a, GraphNode b)
    {
        return Vector3.Distance(
            a.transform.position,
            b.transform.position
        );
    }

    private List<GraphNode> ReconstructPath(
        Dictionary<GraphNode, GraphNode> cameFrom,
        GraphNode current)
    {
        List<GraphNode> path = new List<GraphNode>();

        path.Add(current);

        while (cameFrom.TryGetValue(
            current, out GraphNode previous))
        {
            current = previous;
            path.Add(current);
        }

        path.Reverse();

        return path;
    }
}
