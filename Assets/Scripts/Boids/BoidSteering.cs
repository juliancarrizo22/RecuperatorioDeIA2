
using System.Collections.Generic;
using UnityEngine;

public class BoidSteering : MonoBehaviour
{
    private BoidAgent boidAgent;

    private float maxSpeed;
    private float maxSteering;

    public void Initialize(
        BoidAgent agent,
        float speed,
        float steering)
    {
        boidAgent = agent;
        maxSpeed = speed;
        maxSteering = steering;
    }

    public Vector3 GetSeparation(
        List<BoidAgent> nearbyBoids,
        float separationRadius)
    {
        Vector3 force = Vector3.zero;
        float radiusSquared = separationRadius * separationRadius;

        foreach (BoidAgent other in nearbyBoids)
        {
            if (other == null)
                continue;

            Vector3 away = transform.position - other.transform.position;
            float distanceSquared = away.sqrMagnitude;

            if (distanceSquared > 0.001f &&
                distanceSquared <= radiusSquared)
            {
                force += away.normalized / distanceSquared;
            }
        }

        return SteerTowards(force);
    }

    public Vector3 GetAlignment(
        List<BoidAgent> nearbyBoids,
        float alignmentRadius)
    {
        Vector3 averageVelocity = Vector3.zero;
        float radiusSquared = alignmentRadius * alignmentRadius;
        int neighborCount = 0;

        foreach (BoidAgent other in nearbyBoids)
        {
            if (other == null)
                continue;

            float distanceSquared =
                (other.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared <= radiusSquared)
            {
                averageVelocity += other.Velocity;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
            return Vector3.zero;

        averageVelocity /= neighborCount;

        return SteerTowards(averageVelocity);
    }

    public Vector3 GetCohesion(
        List<BoidAgent> nearbyBoids,
        float cohesionRadius)
    {
        Vector3 center = Vector3.zero;
        float radiusSquared = cohesionRadius * cohesionRadius;
        int neighborCount = 0;

        foreach (BoidAgent other in nearbyBoids)
        {
            if (other == null)
                continue;

            float distanceSquared =
                (other.transform.position - transform.position).sqrMagnitude;

            if (distanceSquared <= radiusSquared)
            {
                center += other.transform.position;
                neighborCount++;
            }
        }

        if (neighborCount == 0)
            return Vector3.zero;

        center /= neighborCount;

        return SteerTowards(center - transform.position);
    }

    public Vector3 GetEvade(HunterAgent hunter)
    {
        Vector3 futurePosition = hunter.transform.position
            + hunter.Velocity * 0.5f;

        Vector3 direction = transform.position - futurePosition;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = Random.insideUnitSphere;

        direction.y = 0f;

        return SteerTowards(direction);
    }

    public Vector3 SteerTowards(Vector3 desired)
    {
        if (desired.sqrMagnitude < 0.001f)
            return Vector3.zero;

        Vector3 desiredVelocity = desired.normalized * maxSpeed;

        return Vector3.ClampMagnitude(
            desiredVelocity - boidAgent.Velocity,
            maxSteering
        );
    }
}

