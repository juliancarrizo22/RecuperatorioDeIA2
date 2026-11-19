
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidSpawner : MonoBehaviour
{
    [Header("Boids")]
    [SerializeField] private BoidAgent boidPrefab;
    [SerializeField] private int boidCount = 6;

    [Header("Spawn")]
    [SerializeField] private float spawnWidth = 15f;
    [SerializeField] private float spawnHeight = 15f;
    [SerializeField] private float respawnDelay = 5f;

    private readonly List<BoidAgent> activeBoids =
        new List<BoidAgent>();

    private readonly HashSet<int> respawningBoids =
        new HashSet<int>();

    private void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("BoidSpawner: falta asignar el prefab.");
            return;
        }

        for (int i = 0; i < boidCount; i++)
        {
            SpawnBoid();
        }

        StartCoroutine(CheckDeadBoids());
    }

    private void SpawnBoid()
    {
        Vector3 position = new Vector3(
            Random.Range(-spawnWidth, spawnWidth),
            1f,
            Random.Range(-spawnHeight, spawnHeight)
        );

        BoidAgent newBoid = Instantiate(
            boidPrefab,
            position,
            Quaternion.identity
        );

        activeBoids.Add(newBoid);
    }

    private IEnumerator CheckDeadBoids()
    {
        WaitForSeconds wait = new WaitForSeconds(0.25f);

        while (true)
        {
            for (int i = activeBoids.Count - 1; i >= 0; i--)
            {
                BoidAgent boid = activeBoids[i];

                if (boid == null)
                {
                    activeBoids.RemoveAt(i);
                    continue;
                }

                BoidHealth health = boid.GetComponent<BoidHealth>();

                if (health != null && health.IsDead)
                {
                    activeBoids.RemoveAt(i);

                    int boidId = boid.GetInstanceID();

                    if (respawningBoids.Add(boidId))
                    {
                        Debug.Log(
                            "[BoidSpawner] Boid muerto. Iniciando respawn."
                        );

                        StartCoroutine(
                            RespawnBoid(boid, boidId, false)
                        );
                    }
                }
            }

            yield return wait;
        }
    }

    public void CollectBoid(BoidAgent boid)
    {
        if (boid == null)
            return;

        int boidId = boid.GetInstanceID();

        activeBoids.Remove(boid);

        if (respawningBoids.Add(boidId))
        {
            StartCoroutine(
                RespawnBoid(boid, boidId, true)
            );
        }
        else
        {
            Destroy(boid.gameObject);
        }

        Debug.Log("[BoidSpawner] Boid recolectado por el Hunter.");
    }

    private IEnumerator RespawnBoid(
        BoidAgent deadBoid,
        int boidId,
        bool destroyImmediately)
    {
        if (destroyImmediately && deadBoid != null)
        {
            Destroy(deadBoid.gameObject);
        }

        yield return new WaitForSeconds(respawnDelay);

        if (deadBoid != null)
        {
            Destroy(deadBoid.gameObject);
        }

        respawningBoids.Remove(boidId);

        SpawnBoid();
    }
}
