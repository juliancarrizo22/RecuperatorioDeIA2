
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HunterNavigation))]
[RequireComponent(typeof(HunterPerception))]
public class HunterFleeController : MonoBehaviour
{
    public enum FleeResult
    {
        InProgress,
        WolfDetected,
        SafeZoneCompleted
    }

    private HunterNavigation hunterNavigation;
    private HunterPerception hunterPerception;

    private AStarPathfinder pathfinder;
    private Transform[] safeZones;

    private float wolfPerceptionInterval;
    private float safeZoneWaitTime;
    private float safeZoneCenterDistance;
    private float escapeRecalculationInterval;

    private float wolfPerceptionTimer;
    private float safeZoneTimer;
    private float escapeRecalculationTimer;

    private Transform selectedSafeZone;

    private bool isInsideSafeZone;
    private bool isFleeing;
    private bool wolvesNearbyAtSafeZone;

    private void Awake()
    {
        hunterNavigation = GetComponent<HunterNavigation>();
        hunterPerception = GetComponent<HunterPerception>();
    }

    public void Initialize(
        AStarPathfinder pathfinder,
        Transform[] safeZones,
        float wolfPerceptionInterval,
        float safeZoneWaitTime,
        float safeZoneCenterDistance,
        float escapeRecalculationInterval)
    {
        this.pathfinder = pathfinder;
        this.safeZones = safeZones;
        this.wolfPerceptionInterval = wolfPerceptionInterval;
        this.safeZoneWaitTime = safeZoneWaitTime;
        this.safeZoneCenterDistance = safeZoneCenterDistance;
        this.escapeRecalculationInterval = escapeRecalculationInterval;

        ResetFlee();
    }

    public bool CheckForWolf()
    {
        if (isFleeing)
            return false;

        wolfPerceptionTimer += Time.deltaTime;

        if (wolfPerceptionTimer < wolfPerceptionInterval)
            return false;

        wolfPerceptionTimer = 0f;

        return hunterPerception.CheckForWolves();
    }

    public FleeResult UpdateFlee()
    {
        if (!isFleeing)
        {
            isFleeing = true;
            selectedSafeZone = null;
            isInsideSafeZone = false;
            safeZoneTimer = 0f;
            escapeRecalculationTimer = 0f;
        }

        if (isInsideSafeZone)
        {
            hunterNavigation.StopMovement();

            wolfPerceptionTimer += Time.deltaTime;

            if (wolfPerceptionTimer >= wolfPerceptionInterval)
            {
                wolfPerceptionTimer = 0f;

                wolvesNearbyAtSafeZone =
                    hunterPerception.CheckForWolves();
            }

            if (wolvesNearbyAtSafeZone)
            {
                safeZoneTimer = 0f;
            }
            else
            {
                safeZoneTimer += Time.deltaTime;
            }

            if (safeZoneTimer >= safeZoneWaitTime)
            {
                Debug.Log(
                    "[Hunter] El refugio está despejado. " +
                    "Retomando actividad."
                );

                ResetFlee();
                return FleeResult.SafeZoneCompleted;
            }

            return FleeResult.InProgress;
        }

        if (selectedSafeZone == null)
        {
            escapeRecalculationTimer += Time.deltaTime;

            if (escapeRecalculationTimer >= escapeRecalculationInterval)
            {
                escapeRecalculationTimer = 0f;
                ComputeBestEscapePath();
            }

            if (selectedSafeZone == null)
            {
                hunterNavigation.StopMovement();
                return FleeResult.InProgress;
            }
        }

        Vector3 safeCenter = GetSafeZoneCenter(selectedSafeZone);

        float distanceToCenter =
            hunterNavigation.GetFlatDistance(
                transform.position,
                safeCenter
            );

        if (distanceToCenter <= safeZoneCenterDistance)
        {
            isInsideSafeZone = true;
            safeZoneTimer = 0f;

            wolfPerceptionTimer = wolfPerceptionInterval;
            wolvesNearbyAtSafeZone = false;

            hunterNavigation.StopMovement();

            Debug.Log(
                $"[Hunter] Llegó al centro del refugio: {selectedSafeZone.name}"
            );

            return FleeResult.InProgress;
        }

        if (hunterNavigation.CurrentPath == null ||
            hunterNavigation.CurrentPath.Count == 0)
        {
            escapeRecalculationTimer += Time.deltaTime;

            if (escapeRecalculationTimer >= escapeRecalculationInterval)
            {
                escapeRecalculationTimer = 0f;
                ComputeBestEscapePath();
            }

            if (hunterNavigation.HasDirectPathToPosition(safeCenter))
                hunterNavigation.MoveTowards(safeCenter);
            else
                hunterNavigation.StopMovement();

            return FleeResult.InProgress;
        }

        hunterNavigation.FollowCurrentPath();

        return FleeResult.InProgress;
    }

    private void ComputeBestEscapePath()
    {
        if (pathfinder == null || NavigationGraph.Instance == null)
            return;

        if (hunterNavigation == null)
            return;

        if (safeZones == null || safeZones.Length == 0)
        {
            Debug.LogError("[Hunter] No hay zonas seguras asignadas.");
            return;
        }

        GraphNode startNode =
            NavigationGraph.Instance.GetNearestNode(transform.position);

        if (startNode == null)
            return;

        List<GraphNode> bestPath = null;
        Transform bestZone = null;
        float bestDistance = Mathf.Infinity;

        foreach (Transform zone in safeZones)
        {
            if (zone == null)
                continue;

            Vector3 center = GetSafeZoneCenter(zone);

            GraphNode goalNode =
                NavigationGraph.Instance.GetNearestNode(center);

            if (goalNode == null)
                continue;

            List<GraphNode> candidatePath =
                pathfinder.FindPath(startNode, goalNode);

            if (candidatePath == null || candidatePath.Count == 0)
                continue;

            float pathDistance =
                hunterNavigation.CalculatePathDistance(candidatePath);

            if (pathDistance < bestDistance)
            {
                bestDistance = pathDistance;
                bestPath = candidatePath;
                bestZone = zone;
            }
        }

        if (bestPath != null && bestZone != null)
        {
            hunterNavigation.SetPath(bestPath);

            selectedSafeZone = bestZone;

            isInsideSafeZone = false;
            safeZoneTimer = 0f;

            Debug.Log(
                $"[Hunter] Refugio seleccionado: {selectedSafeZone.name}"
            );
        }
        else
        {
            hunterNavigation.ClearPath();

            selectedSafeZone = null;

            Debug.LogWarning(
                "[Hunter] No se encontró una ruta hacia ningún refugio."
            );
        }
    }

    private Vector3 GetSafeZoneCenter(Transform zone)
    {
        if (zone == null)
            return transform.position;

        Collider zoneCollider = zone.GetComponent<Collider>();

        if (zoneCollider == null)
            zoneCollider = zone.GetComponentInChildren<Collider>();

        if (zoneCollider != null)
            return zoneCollider.bounds.center;

        return zone.position;
    }

    public void ResetFlee()
    {
        wolfPerceptionTimer = 0f;
        safeZoneTimer = 0f;
        escapeRecalculationTimer = 0f;

        selectedSafeZone = null;

        isInsideSafeZone = false;
        isFleeing = false;

        wolvesNearbyAtSafeZone = false;
    }
}

