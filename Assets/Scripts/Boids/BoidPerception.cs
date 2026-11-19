
using System.Collections.Generic;
using UnityEngine;

public class BoidPerception : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField] private int bufferSize = 64;

    private float perceptionRadius;
    private Collider[] perceptionBuffer;

    private readonly List<BoidAgent> nearbyBoids = new List<BoidAgent>();
    private readonly List<InterestObject> nearbyInterests = new List<InterestObject>();

    private HunterAgent nearbyHunter;

    public List<BoidAgent> NearbyBoids => nearbyBoids;
    public List<InterestObject> NearbyInterests => nearbyInterests;
    public HunterAgent NearbyHunter => nearbyHunter;

    private void Awake()
    {
        perceptionBuffer = new Collider[bufferSize];
    }

    public void Initialize(float radius)
    {
        perceptionRadius = radius;
    }

    public void RefreshPerception()
    {
        nearbyBoids.Clear();
        nearbyInterests.Clear();
        nearbyHunter = null;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            perceptionRadius,
            perceptionBuffer
        );

        float closestHunterDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider detectedCollider = perceptionBuffer[i];

            if (detectedCollider == null)
                continue;

            BoidAgent boid = detectedCollider.GetComponentInParent<BoidAgent>();

            if (boid != null && boid != GetComponent<BoidAgent>())
            {
                if (!nearbyBoids.Contains(boid))
                    nearbyBoids.Add(boid);
            }

            HunterAgent hunter = detectedCollider.GetComponentInParent<HunterAgent>();

            if (hunter != null)
            {
                float distance = (hunter.transform.position - transform.position).sqrMagnitude;

                if (distance < closestHunterDistance)
                {
                    closestHunterDistance = distance;
                    nearbyHunter = hunter;
                }
            }

            InterestObject interest = detectedCollider.GetComponentInParent<InterestObject>();

            if (interest != null && !nearbyInterests.Contains(interest))
            {
                nearbyInterests.Add(interest);
            }
        }
    }
}
