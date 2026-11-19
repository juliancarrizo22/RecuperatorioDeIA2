
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WolfPerception))]
public class WolfNavigation : MonoBehaviour
{
    [Header("Movement")]
    private float speed;
    private float nodeReachDistance;

    [Header("Path Recalculation")]
    private float pathRecalculationInterval;
    private float destinationRecalculationThreshold;

    private AStarPathfinder pathfinder;
    private WolfPerception wolfPerception;
    private CharacterController characterController;

    private List<GraphNode> currentPath = new List<GraphNode>();
    private int currentPathIndex;

    private GraphNode lastGoalNode;
    private float pathRecalculationTimer;

    private Vector3 lastDestinationChecked = Vector3.negativeInfinity;

    private bool hasValidPath;

    public void Initialize(
        AStarPathfinder pathfinder,
        float movementSpeed,
        float reachDistance,
        float recalculationInterval,
        float destinationThreshold)
    {
        this.pathfinder = pathfinder;
        speed = movementSpeed;
        nodeReachDistance = reachDistance;
        pathRecalculationInterval = recalculationInterval;
        destinationRecalculationThreshold = destinationThreshold;

        wolfPerception = GetComponent<WolfPerception>();
        characterController = GetComponent<CharacterController>();

        ResetNavigation();
    }

    public void ResetNavigation()
    {
        currentPath.Clear();
        currentPathIndex = 0;

        lastGoalNode = null;
        lastDestinationChecked = Vector3.negativeInfinity;

        hasValidPath = false;

        pathRecalculationTimer = pathRecalculationInterval;
    }

    public void MoveAlongPath(Vector3 destination)
    {
        if (pathfinder == null || NavigationGraph.Instance == null)
        {
            StopMovement();
            return;
        }

        pathRecalculationTimer += Time.deltaTime;

        bool destinationMoved =
            Vector3.Distance(
                lastDestinationChecked,
                destination
            ) > destinationRecalculationThreshold;

        GraphNode goalNode =
            NavigationGraph.Instance.GetNearestNodeForWolves(destination);

        if (goalNode == null)
        {
            StopMovement();
            return;
        }

        bool goalChanged = goalNode != lastGoalNode;

        bool pathMissing = !hasValidPath;

        bool needsNewPath =
            pathMissing
                ? pathRecalculationTimer >= pathRecalculationInterval
                : goalChanged ||
                  (destinationMoved &&
                   pathRecalculationTimer >= pathRecalculationInterval);

        if (needsNewPath)
        {
            GraphNode startNode =
                NavigationGraph.Instance.GetNearestNodeForWolves(transform.position);

            if (startNode == null)
            {
                StopMovement();
                return;
            }

            List<GraphNode> newPath = GetFilteredPath(startNode, goalNode);

            pathRecalculationTimer = 0f;
            lastDestinationChecked = destination;
            lastGoalNode = goalNode;

            if (newPath == null || newPath.Count == 0)
            {
                currentPath.Clear();
                currentPathIndex = 0;
                hasValidPath = false;

                StopMovement();

                Debug.LogWarning(
                    $"[WolfNavigation] {gameObject.name}: A* no encontró una ruta."
                );

                return;
            }

            currentPath = newPath;
            currentPathIndex = 0;
            hasValidPath = true;

            if (currentPath.Count > 1)
            {
                bool canSkipStartNode =
                    NavigationGraph.Instance.IsSegmentAllowedForWolves(
                        transform.position,
                        currentPath[1].transform.position
                    )
                    &&
                    wolfPerception.HasLineOfSight(
                        currentPath[1].transform.position
                    );

                if (canSkipStartNode)
                    currentPathIndex = 1;
            }
        }

        if (!hasValidPath)
        {
            StopMovement();
            return;
        }

        while (currentPathIndex < currentPath.Count)
        {
            GraphNode node = currentPath[currentPathIndex];

            if (node == null)
            {
                currentPathIndex++;
                continue;
            }

            float distance = GetFlatDistance(
                transform.position,
                node.transform.position
            );

            if (distance <= nodeReachDistance)
                currentPathIndex++;
            else
                break;
        }

        if (currentPathIndex < currentPath.Count)
        {
            MoveToward(
                currentPath[currentPathIndex].transform.position
            );

            return;
        }

        bool destinationAllowed =
            NavigationGraph.Instance.IsPositionAllowedForWolves(destination);

        bool segmentAllowed =
            NavigationGraph.Instance.IsSegmentAllowedForWolves(
                transform.position,
                destination
            );

        bool hasClearSight =
            wolfPerception.HasLineOfSight(destination);

        if (destinationAllowed && segmentAllowed && hasClearSight)
            MoveToward(destination);
        else
            StopMovement();
    }

    private List<GraphNode> GetFilteredPath(
        GraphNode start,
        GraphNode goal)
    {
        if (pathfinder == null ||
            NavigationGraph.Instance == null)
        {
            return null;
        }

        return pathfinder.FindPath(
            start,
            goal,
            node => NavigationGraph.Instance.IsNodeAllowedForWolves(node),
            null,
            (from, to) =>
                NavigationGraph.Instance.IsSegmentAllowedForWolves(
                    from.transform.position,
                    to.transform.position
                )
        );
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void MoveToward(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            StopMovement();
            return;
        }

        direction.Normalize();

        Vector3 velocity = direction * speed;

        if (characterController != null)
            characterController.Move(velocity * Time.deltaTime);
        else
            transform.position += velocity * Time.deltaTime;

        transform.forward = direction;
    }

    public void StopMovement()
    {

    }

    public bool HasPath()
    {
        return hasValidPath;
    }

    public bool HasFinishedPath()
    {
        return hasValidPath &&
               currentPathIndex >= currentPath.Count;
    }

    public bool HasCompletedPath()
    {
        return hasValidPath &&
               currentPath.Count > 0 &&
               currentPathIndex >= currentPath.Count;
    }

    private void OnDisable()
    {
        currentPath.Clear();
        currentPathIndex = 0;
        hasValidPath = false;
    }
}
