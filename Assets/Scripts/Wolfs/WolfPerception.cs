
using UnityEngine;

public class WolfPerception : MonoBehaviour
{
    [Header("Detection")]
    private float viewRadius;
    private float viewAngle;
    private LayerMask obstacleMask;
    private LayerMask hunterMask;

    private readonly Collider[] hunterDetectionBuffer = new Collider[32];

    public void Initialize(
        float radius,
        float angle,
        LayerMask obstacles,
        LayerMask hunters)
    {
        viewRadius = radius;
        viewAngle = angle;
        obstacleMask = obstacles;
        hunterMask = hunters;
    }

    public HunterAgent CheckForHunter()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(
            transform.position,
            viewRadius,
            hunterDetectionBuffer,
            hunterMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = hunterDetectionBuffer[i];

            if (hit == null)
                continue;

            HunterAgent hunter =
                hit.GetComponentInParent<HunterAgent>();

            if (hunter == null)
                continue;

            if (NavigationGraph.Instance != null &&
                !NavigationGraph.Instance.IsPositionAllowedForWolves(
                    hunter.transform.position))
            {
                continue;
            }

            Vector3 direction =
                hunter.transform.position - transform.position;

            direction.y = 0f;

            float angle = Vector3.Angle(
                transform.forward,
                direction.normalized
            );

            if (angle > viewAngle / 2f)
                continue;

            if (HasLineOfSight(hunter.transform.position))
                return hunter;
        }

        return null;
    }

    public bool HasLineOfSight(Vector3 targetPosition)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = targetPosition - origin;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            direction.magnitude,
            obstacleMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        forward.Normalize();

        Vector3 leftBoundary =
            Quaternion.Euler(0f, -viewAngle * 0.5f, 0f) * forward;

        Vector3 rightBoundary =
            Quaternion.Euler(0f, viewAngle * 0.5f, 0f) * forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawLine(
            origin,
            origin + leftBoundary * viewRadius
        );

        Gizmos.DrawLine(
            origin,
            origin + rightBoundary * viewRadius
        );

        int segments = 30;

        Vector3 previousPoint =
            origin + leftBoundary * viewRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                -viewAngle * 0.5f +
                (viewAngle * i / segments);

            Vector3 direction =
                Quaternion.Euler(0f, angle, 0f) * forward;

            Vector3 point =
                origin + direction * viewRadius;

            Gizmos.DrawLine(previousPoint, point);

            previousPoint = point;
        }
    }
}
