
using System.Collections.Generic;
using UnityEngine;

public class HunterPerception : MonoBehaviour
{
    [Header("Boid Perception")]
    [SerializeField] private float perceptionRadius = 12f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Wolf Perception")]
    [SerializeField] private float wolfPerceptionRadius = 15f;

    private readonly Collider[] boidDetectionBuffer = new Collider[64];
    private readonly List<Transform> detectedWolves = new List<Transform>();

    private bool wasDetectingWolves;

    public float PerceptionRadius => perceptionRadius;
    public float WolfPerceptionRadius => wolfPerceptionRadius;

    public Transform FindBestVisibleBoid(float attackCooldown)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            perceptionRadius,
            boidDetectionBuffer,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Transform bestDeadBoid = null;
        Transform bestLiveBoid = null;

        float bestDeadDistance = Mathf.Infinity;
        float bestLiveDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = boidDetectionBuffer[i];

            if (hit == null)
                continue;

            BoidAgent boid = hit.GetComponentInParent<BoidAgent>();

            if (boid == null)
                continue;

            BoidHealth health = boid.GetComponent<BoidHealth>();

            if (health == null || !HasLineOfSight(boid.transform))
                continue;

            float distance = GetFlatDistance(
                transform.position,
                boid.transform.position
            );

            if (health.IsDead)
            {
                if (distance < bestDeadDistance)
                {
                    bestDeadDistance = distance;
                    bestDeadBoid = boid.transform;
                }
            }
            else if (attackCooldown <= 0f && distance < bestLiveDistance)
            {
                bestLiveDistance = distance;
                bestLiveBoid = boid.transform;
            }
        }

        return bestDeadBoid != null ? bestDeadBoid : bestLiveBoid;
    }

    public bool CheckForWolves()
    {
        detectedWolves.Clear();

        WolfManager manager = WolfManager.Instance;

        if (manager == null)
        {
            wasDetectingWolves = false;
            return false;
        }

        foreach (WolfAgent wolf in manager.Wolves)
        {
            if (wolf == null)
                continue;

            float distance = GetFlatDistance(
                transform.position,
                wolf.transform.position
            );

            if (distance > wolfPerceptionRadius)
                continue;

            if (!HasLineOfSight(wolf.transform))
                continue;

            detectedWolves.Add(wolf.transform);
        }

        bool detected = detectedWolves.Count > 0;

        if (detected != wasDetectingWolves)
        {
            Debug.Log(
                detected
                    ? $"[Hunter] Detectó {detectedWolves.Count} lobo(s)."
                    : "[Hunter] Ya no detecta lobos."
            );
        }

        wasDetectingWolves = detected;

        return detected;
    }

    public bool HasLineOfSight(Transform target)
    {
        if (target == null)
            return false;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 destination = target.position + Vector3.up * 0.5f;
        Vector3 direction = destination - origin;

        if (direction.sqrMagnitude <= 0.001f)
            return true;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    public Vector3 GetPursuitPosition(
        Transform target,
        float hunterSpeed,
        float predictionMultiplier,
        float maxPredictionTime)
    {
        if (target == null)
            return transform.position;

        BoidAgent boid = target.GetComponentInParent<BoidAgent>();

        if (boid == null)
            return target.position;

        Vector3 targetVelocity = boid.Velocity;
        targetVelocity.y = 0f;

        Vector3 directionToTarget = target.position - transform.position;
        directionToTarget.y = 0f;

        float predictionTime =
            directionToTarget.magnitude / Mathf.Max(hunterSpeed, 0.1f);

        predictionTime *= predictionMultiplier;

        predictionTime = Mathf.Clamp(
            predictionTime,
            0f,
            maxPredictionTime
        );

        Vector3 predictedPosition =
            target.position + targetVelocity * predictionTime;

        predictedPosition.y = target.position.y;

        return predictedPosition;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }

    public void ResetPerception()
    {
        detectedWolves.Clear();
        wasDetectingWolves = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, wolfPerceptionRadius);
    }
}

