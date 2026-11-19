using UnityEngine;

[RequireComponent(typeof(BoidPerception))]
[RequireComponent(typeof(BoidSteering))]
[RequireComponent(typeof(BoidMovement))]
public class BoidAgent : MonoBehaviour
{
    [Header("Hunter")]
    [SerializeField] private float hunterPerceptionRadius = 10f;
    [SerializeField] private float evadeWeight = 2f;

    [Header("Interest")]
    [SerializeField] private float interestPerceptionRadius = 12f;
    [SerializeField] private float arriveSlowingDistance = 3f;
    [SerializeField] private float interestReachDistance = 0.8f;

    [Header("Movement")]
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float maxSteering = 3f;

    [Header("Boundary")]
    [SerializeField] private float boundaryMargin = 2f;
    [SerializeField] private float boundaryWeight = 4f;

    [Header("Perception")]
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float alignmentRadius = 5f;
    [SerializeField] private float cohesionRadius = 5f;
    [SerializeField] private float perceptionInterval = 0.15f;

    [Header("Weights")]
    [SerializeField] private float separationWeight = 1f;
    [SerializeField] private float alignmentWeight = 1f;
    [SerializeField] private float cohesionWeight = 1f;

    private BoidHealth health;
    private BoidPerception boidPerception;
    private BoidSteering boidSteering;
    private BoidMovement boidMovement;
   public Vector3 Velocity => boidMovement.Velocity;

    private float perceptionTimer;
    private float perceptionRadius;

    private void Awake()
    {
        health = GetComponent<BoidHealth>();
        boidPerception = GetComponent<BoidPerception>();
        boidSteering = GetComponent<BoidSteering>();
        boidMovement = GetComponent<BoidMovement>();

        perceptionRadius = Mathf.Max(
            hunterPerceptionRadius,
            interestPerceptionRadius,
            cohesionRadius,
            alignmentRadius,
            separationRadius
        );

        boidPerception.Initialize(perceptionRadius);

        boidSteering.Initialize(
            this,
            maxSpeed,
            maxSteering
        );

        boidMovement.Initialize(maxSpeed);
    }

    private void Start()
    {
        perceptionTimer = perceptionInterval;
    }

    public void InitializeMovement()
    {
        boidMovement.InitializeMovement();
        perceptionTimer = perceptionInterval;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            boidMovement.StopMovement();
            return;
        }

        perceptionTimer += Time.deltaTime;

        if (perceptionTimer >= perceptionInterval)
        {
            boidPerception.RefreshPerception();
            perceptionTimer = 0f;
        }

        Vector3 steering;

        if (boidPerception.NearbyHunter != null &&
            Vector3.Distance(transform.position,
                boidPerception.NearbyHunter.transform.position) <= hunterPerceptionRadius)
        {
            steering = boidSteering.GetEvade(boidPerception.NearbyHunter) * evadeWeight;
        }
        else
        {
            InterestObject interest = FindInterest();

            if (interest != null)
            {
                steering = GetArrive(interest.transform);

                if (Vector3.Distance(
                    transform.position,
                    interest.transform.position) <= interestReachDistance)
                {
                    if (InterestManager.Instance != null)
                        InterestManager.Instance.RemoveInterest(interest);
                }
            }
            else
            {
                steering = boidSteering.GetSeparation(boidPerception.NearbyBoids,separationRadius) * separationWeight 
                + boidSteering.GetAlignment(boidPerception.NearbyBoids,alignmentRadius) * alignmentWeight + 
                boidSteering.GetCohesion(boidPerception.NearbyBoids,cohesionRadius) * cohesionWeight;
            }
        }

        steering += GetBoundarySteering() * boundaryWeight;

        steering = Vector3.ClampMagnitude(steering, maxSteering);

        boidMovement.ApplySteering(steering);

        KeepInsideBounds();
    }
    
    private InterestObject FindInterest()
    {
        InterestObject closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (InterestObject interest in boidPerception.NearbyInterests)
        {
            if (interest == null)
                continue;

            float distance = Vector3.Distance(
                transform.position,
                interest.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = interest;
            }
        }

        return closest;
    }

    private Vector3 GetArrive(Transform target)
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance < 0.01f)
            return -boidMovement.Velocity;

        float desiredSpeed = maxSpeed;

        if (distance < arriveSlowingDistance)
            desiredSpeed *= distance / arriveSlowingDistance;

        Vector3 desiredVelocity = direction.normalized * desiredSpeed;

        return Vector3.ClampMagnitude(
            desiredVelocity - boidMovement.Velocity,
            maxSteering
        );
    }

    private void KeepInsideBounds()
    {
        if (BoidBounds.Instance == null)
            return;

        Vector3 correctedPosition = BoidBounds.Instance.KeepInsideBounds(
            transform.position,
            ref boidMovement.GetVelocityReference()
        );

        boidMovement.SetPosition(correctedPosition);
    }

    private void OnDrawGizmosSelected()
    {
        // Separation
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        // Alignment
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, alignmentRadius);

        // Cohesion
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, cohesionRadius);

        // Perception general
        float gizmoPerceptionRadius = Mathf.Max(
            hunterPerceptionRadius,
            interestPerceptionRadius,
            separationRadius,
            alignmentRadius,
            cohesionRadius
        );

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(
            transform.position,
            gizmoPerceptionRadius
        );
    }

    private Vector3 GetBoundarySteering()
    {
        if (BoidBounds.Instance == null)
            return Vector3.zero;

        Vector3 center = BoidBounds.Instance.transform.position;
        Vector3 localPosition = transform.position - center;

        Vector2 halfSize = BoidBounds.Instance.BoundsSize / 2f;

        Vector3 force = Vector3.zero;

        if (localPosition.x > halfSize.x - boundaryMargin)
            force.x -= 1f;
        else if (localPosition.x < -halfSize.x + boundaryMargin)
            force.x += 1f;

        if (localPosition.z > halfSize.y - boundaryMargin)
            force.z -= 1f;
        else if (localPosition.z < -halfSize.y + boundaryMargin)
            force.z += 1f;

        return boidSteering.SteerTowards(force);
    }
}


