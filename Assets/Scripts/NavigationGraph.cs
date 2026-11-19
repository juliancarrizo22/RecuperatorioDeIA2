
using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    public static NavigationGraph Instance { get; private set; }

    [SerializeField]
    private List<GraphNode> nodes = new List<GraphNode>();

    [SerializeField]
    private NavigationZone[] restrictedZones;

    public IReadOnlyList<GraphNode> Nodes => nodes;

    private void Awake()
    {
        Instance = this;

        if (nodes.Count == 0)
            nodes.AddRange(GetComponentsInChildren<GraphNode>());

        if (restrictedZones == null || restrictedZones.Length == 0)
        {

        }

    }
    
    public bool IsPositionAllowedForWolves(Vector3 position)
    {
        if (restrictedZones == null)
            return true;

        foreach (NavigationZone zone in restrictedZones)
        {
            if (zone == null || !zone.BlocksWolves)
                continue;

            if (zone.ContainsPositionXZ(position))
                return false;
        }

        return true;
    }

    public bool IsNodeAllowedForWolves(GraphNode node)
    {
        if (node == null)
            return false;

        return IsPositionAllowedForWolves(
            node.transform.position
        );
    }

    public GraphNode GetNearestNode(Vector3 position)
    {
        return GetNearestNodeInternal(position, null, false);
    }

    public GraphNode GetNearestNode(
        Vector3 position,
        GraphNode excludedNode)
    {
        return GetNearestNodeInternal(position, excludedNode, false);
    }

    public GraphNode GetNearestNodeForWolves(Vector3 position)
    {
        return GetNearestNodeInternal(position, null, true);
    }

    private GraphNode GetNearestNodeInternal(
        Vector3 position,
        GraphNode excludedNode,
        bool filterForWolves)
    {
        GraphNode nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GraphNode node in nodes)
        {
            if (node == null || node == excludedNode)
                continue;

            if (filterForWolves && !IsNodeAllowedForWolves(node))
                continue;

            Vector3 nodePosition = node.transform.position;

            float dx = position.x - nodePosition.x;
            float dz = position.z - nodePosition.z;

            float distanceSquared = dx * dx + dz * dz;

            if (distanceSquared < shortestDistance)
            {
                shortestDistance = distanceSquared;
                nearest = node;
            }
        }

        return nearest;
    }

    public bool IsSegmentAllowedForWolves(
        Vector3 start,
        Vector3 end)
    {
        float distance = Vector3.Distance(start, end);

        int samples = Mathf.Max(
            2,
            Mathf.CeilToInt(distance / 0.5f)
        );

        for (int i = 1; i < samples; i++)
        {
            float t = (float)i / samples;

            Vector3 point = Vector3.Lerp(start, end, t);

            if (!IsPositionAllowedForWolves(point))
                return false;
        }

        return true;
    }
}
