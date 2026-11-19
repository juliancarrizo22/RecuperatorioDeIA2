
using UnityEngine;

[RequireComponent(typeof(HunterNavigation))]
[RequireComponent(typeof(HunterPerception))]
public class HunterGatherController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoidSpawner boidSpawner;

    private HunterNavigation hunterNavigation;
    private HunterPerception hunterPerception;

    private float gatherDistance;
    private float gatherTime;
    private float gatherTimer;

    private Transform currentGatherTarget;

    private bool hasReachedTarget;

    public enum GatherResult
    {
        InProgress,
        Completed,
        Invalid
    }

    private void Awake()
    {
        hunterNavigation = GetComponent<HunterNavigation>();
        hunterPerception = GetComponent<HunterPerception>();
    }

    public void Initialize(float distance, float time)
    {
        gatherDistance = distance;
        gatherTime = time;

        ResetGather();
    }

    public GatherResult UpdateGather(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            ResetGather();
            return GatherResult.Invalid;
        }

        BoidHealth health = target.GetComponentInParent<BoidHealth>();

        if (health == null || !health.IsDead)
        {
            ResetGather();
            return GatherResult.Invalid;
        }

        if (currentGatherTarget != target)
        {
            ResetGather();
            currentGatherTarget = target;
        }

        if (!hasReachedTarget)
        {
            float distance = GetFlatDistance(
                transform.position,
                target.position
            );

            if (distance > gatherDistance)
            {
                if (hunterPerception.HasLineOfSight(target))
                    hunterNavigation.MoveTowards(target.position);
                else
                    hunterNavigation.NavigateToPosition(target.position);

                return GatherResult.InProgress;
            }

            hasReachedTarget = true;
            gatherTimer = 0f;
        }

        hunterNavigation.StopMovement();
        hunterNavigation.RotateTowards(target.position);

        gatherTimer += Time.deltaTime;

        if (gatherTimer >= gatherTime)
        {
            BoidAgent boid = target.GetComponentInParent<BoidAgent>();

            if (boidSpawner != null && boid != null)
            {
                boidSpawner.CollectBoid(boid);
            }
            else
            {
                Debug.LogError(
                    "[HunterGather] No se pudo notificar la recolección al BoidSpawner. " +
                    "Comprueba que esté asignado en el Inspector."
                );
            }

            ResetGather();
            return GatherResult.Completed;
        }

        return GatherResult.InProgress;
    }

    public void ResetGather()
    {
        gatherTimer = 0f;
        currentGatherTarget = null;
        hasReachedTarget = false;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }
}
