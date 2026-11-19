
using System.Collections.Generic;
using UnityEngine;

public class HunterNavigation : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 4f;
    [SerializeField] private float nodeReachDistance = 0.5f;

    private AStarPathfinder pathfinder;
    private CharacterController characterController;

    private List<GraphNode> searchNodes;
    private Collider huntingZone;

    private List<GraphNode> currentPath = new List<GraphNode>();

    private int searchNodeIndex;
    private Vector3 currentVelocity;

    public Vector3 Velocity => currentVelocity;
    public List<GraphNode> CurrentPath => currentPath;
    public int SearchNodeIndex => searchNodeIndex;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void Initialize(
        AStarPathfinder pathfinder,
        List<GraphNode> searchNodes,
        Collider huntingZone,
        float speed,
        float nodeReachDistance)
    {
        this.pathfinder = pathfinder;
        this.searchNodes = searchNodes;
        this.huntingZone = huntingZone;
        this.speed = speed;
        this.nodeReachDistance = nodeReachDistance;

        searchNodeIndex = 0;
        currentPath.Clear();
    }

    public void SetSearchNodeIndex(int index)
    {
        searchNodeIndex = index;
    }

    public int GetNearestSearchNodeIndex()
    {
        if (searchNodes == null || searchNodes.Count == 0)
            return 0;

        int bestIndex = 0;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < searchNodes.Count; i++)
        {
            if (searchNodes[i] == null)
                continue;

            float distance = GetFlatDistance(
                transform.position,
                searchNodes[i].transform.position
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    public bool NavigateSearch()
    {
        if (searchNodes == null || searchNodes.Count == 0)
        {
            StopMovement();
            return false;
        }

        if (searchNodeIndex < 0 || searchNodeIndex >= searchNodes.Count)
            searchNodeIndex = GetNearestSearchNodeIndex();

        if (currentPath == null || currentPath.Count == 0)
            BuildPathToSearchNode(searchNodeIndex);

        if (FollowCurrentPath())
        {
            searchNodeIndex = (searchNodeIndex + 1) % searchNodes.Count;
            BuildPathToSearchNode(searchNodeIndex);
            return true;
        }

        return false;
    }

    private void BuildPathToSearchNode(int index)
    {
        if (pathfinder == null || NavigationGraph.Instance == null ||
            searchNodes == null || searchNodes.Count == 0)
            return;

        if (index < 0 || index >= searchNodes.Count ||
            searchNodes[index] == null)
            return;

        GraphNode startNode =
            NavigationGraph.Instance.GetNearestNode(transform.position);

        GraphNode goalNode = searchNodes[index];

        if (startNode != null && goalNode != null)
            currentPath = pathfinder.FindPath(startNode, goalNode);
    }

    public bool NavigateToExit(Transform exitZone)
    {
        if (exitZone == null)
        {
            StopMovement();
            return false;
        }

        if (GetFlatDistance(transform.position, exitZone.position) <= 1.5f)
        {
            StopMovement();
            return true;
        }

        if (currentPath == null || currentPath.Count == 0)
            BuildPathToExit(exitZone);

        FollowCurrentPath();
        return false;
    }

    private void BuildPathToExit(Transform exitZone)
    {
        if (pathfinder == null || NavigationGraph.Instance == null ||
            exitZone == null)
            return;

        GraphNode startNode =
            NavigationGraph.Instance.GetNearestNode(transform.position);

        GraphNode goalNode =
            NavigationGraph.Instance.GetNearestNode(exitZone.position);

        if (startNode != null && goalNode != null)
            currentPath = pathfinder.FindPath(startNode, goalNode);
    }

    public void NavigateToPosition(Vector3 targetPosition)
    {
        if (HasDirectPathToPosition(targetPosition))
        {
            currentPath.Clear();
            MoveTowards(targetPosition);
            return;
        }

        if (currentPath == null || currentPath.Count == 0)
            BuildPathToPosition(targetPosition);

        FollowCurrentPath();
    }

    public bool HasDirectPathToPosition(Vector3 targetPosition)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 destination = targetPosition + Vector3.up * 0.5f;
        Vector3 direction = destination - origin;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            GetObstacleMask(),
            QueryTriggerInteraction.Ignore
        );
    }

    private LayerMask GetObstacleMask()
    {
        return obstacleMask;
    }

    [SerializeField] private LayerMask obstacleMask;

    public void SetObstacleMask(LayerMask mask)
    {
        obstacleMask = mask;
    }

    private void BuildPathToPosition(Vector3 targetPosition)
    {
        if (pathfinder == null || NavigationGraph.Instance == null)
            return;

        GraphNode startNode =
            NavigationGraph.Instance.GetNearestNode(transform.position);

        GraphNode goalNode =
            NavigationGraph.Instance.GetNearestNode(targetPosition);

        if (startNode != null && goalNode != null)
            currentPath = pathfinder.FindPath(startNode, goalNode);
    }

    public bool FollowCurrentPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            StopMovement();
            return true;
        }

        while (currentPath.Count > 0)
        {
            GraphNode node = currentPath[0];

            if (node == null)
            {
                currentPath.RemoveAt(0);
                continue;
            }

            if (GetFlatDistance(transform.position, node.transform.position)
                <= nodeReachDistance)
            {
                currentPath.RemoveAt(0);
                continue;
            }

            MoveTowards(node.transform.position);
            return false;
        }

        StopMovement();
        return true;
    }

    public void MoveTowards(Vector3 target)
    {
        if (characterController == null || !characterController.enabled)
        {
            StopMovement();
            return;
        }

        Vector3 flatTarget = new Vector3(
            target.x,
            transform.position.y,
            target.z
        );

        Vector3 direction = flatTarget - transform.position;
        float distance = direction.magnitude;

        if (distance <= 0.01f)
        {
            StopMovement();
            return;
        }

        direction /= distance;

        float movementDistance = Mathf.Min(
            speed * Time.deltaTime,
            distance
        );

        Vector3 movement = direction * movementDistance;
        Vector3 previousPosition = transform.position;

        characterController.Move(movement);

        currentVelocity =
            (transform.position - previousPosition) /
            Mathf.Max(Time.deltaTime, 0.0001f);

        RotateTowards(flatTarget);
    }

    public void RotateTowards(Vector3 target)
    {
        Vector3 flatTarget = new Vector3(
            target.x,
            transform.position.y,
            target.z
        );

        Vector3 direction = flatTarget - transform.position;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            10f * Time.deltaTime
        );
    }

    public void StopMovement()
    {
        currentVelocity = Vector3.zero;
    }

    public void TeleportTo(Vector3 position)
    {
        bool controllerWasEnabled =
            characterController != null && characterController.enabled;

        if (controllerWasEnabled)
            characterController.enabled = false;

        transform.position = position;

        if (controllerWasEnabled)
            characterController.enabled = true;

        StopMovement();
        ClearPath();
    }

    public void ClearPath()
    {
        currentPath.Clear();
    }

    public void ResetNavigation(int nearestSearchNode = 0)
    {
        currentPath.Clear();
        searchNodeIndex = nearestSearchNode;
        StopMovement();
    }

    public float CalculatePathDistance(List<GraphNode> path)
    {
        if (path == null || path.Count == 0)
            return Mathf.Infinity;

        float distance =
            GetFlatDistance(transform.position, path[0].transform.position);

        for (int i = 1; i < path.Count; i++)
        {
            if (path[i - 1] == null || path[i] == null)
                continue;

            distance += GetFlatDistance(
                path[i - 1].transform.position,
                path[i].transform.position
            );
        }

        return distance;
    }

    public float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }

    public bool IsInsideHuntingZone()
    {
        if (huntingZone == null)
            return false;

        return huntingZone.bounds.Contains(transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentPath == null || currentPath.Count == 0)
            return;

        Gizmos.color = Color.cyan;
        Vector3 previous = transform.position;

        foreach (GraphNode node in currentPath)
        {
            if (node == null)
                continue;

            Gizmos.DrawLine(previous, node.transform.position);
            previous = node.transform.position;
        }
    }

    public void SetPath(List<GraphNode> path)
    {
        currentPath = path != null
            ? new List<GraphNode>(path)
            : new List<GraphNode>();
    }
}

