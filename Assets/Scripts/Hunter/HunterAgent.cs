using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HunterPerception))]
[RequireComponent(typeof(HunterNavigation))]
[RequireComponent(typeof(HunterLifecycle))]
[RequireComponent(typeof(HunterCombat))]
[RequireComponent(typeof(HunterGatherController))]
[RequireComponent(typeof(HunterFleeController))]
public class HunterAgent : MonoBehaviour
{
    public enum HunterState
    {
        Search,
        Attack,
        Gather,
        Flee,
        Exit,
        Dead
    }

    [Header("Movement")]
    public float speed = 4f;

    [Header("Navigation")]
    public AStarPathfinder pathfinder;
    public List<GraphNode> searchNodes = new List<GraphNode>();
    public float nodeReachDistance = 0.5f;
    public Collider huntingZone;

    [Header("Combat")]
    public float tba = 3f;
    public float rangeAttackRadius = 4f;
    public float meleeAttackRadius = 2f;
    public int attackDamage = 1;
    public LayerMask obstacleMask;

    [Header("Gather")]
    public float gatherDistance = 1.5f;
    public float gatherTime = 2f;

    [Header("Respawn")]
    public float respawnTime = 5f;
    public Transform initialZoneTransform;

    [Header("Interest Spawning")]
    public float interestSpawnInterval = 5f;
    private float interestSpawnTimer;

    [Header("Flee & Exit")]
    public Transform exitZone;
    public int requiredGathers = 3;

    [Header("Flee Configuration")]
    [SerializeField] private Transform[] safeZones;
    [SerializeField] private float wolfPerceptionInterval = 0.15f;
    [SerializeField] private float safeZoneWaitTime = 4f;
    [SerializeField] private float safeZoneCenterDistance = 0.6f;
    [SerializeField] private float escapeRecalculationInterval = 0.5f;

    [Header("Pursuit")]
    [SerializeField] private float pursuitPredictionMultiplier = 1f;
    [SerializeField] private float maxPursuitPredictionTime = 1.25f;

    [Header("Perception")]
    public float perceptionInterval = 0.15f;
    public float lostTargetTolerance = 1.5f;

    private HunterState currentState = HunterState.Search;

    private HunterVisualFeedback visualFeedback;

    private Transform currentTarget;

    private Vector3 lastKnownTargetPos;

    private int currentGathers;

    private float perceptionTimer;
    private float rangedAttackCooldown;
    private float meleeAttackCooldown;
    private float lostTargetTimer;

    private HunterPerception hunterPerception;
    private HunterNavigation hunterNavigation;
    private HunterLifecycle hunterLifecycle;
    private HunterCombat hunterCombat;
    private HunterGatherController hunterGatherController;
    private HunterFleeController hunterFleeController;

    public HunterState CurrentState => currentState;
    public int CurrentGathers => currentGathers;
    public Vector3 Velocity => hunterNavigation != null? hunterNavigation.Velocity: Vector3.zero;

    private void Awake()
    {

        hunterPerception = GetComponent<HunterPerception>();
        hunterNavigation = GetComponent<HunterNavigation>();
        hunterLifecycle = GetComponent<HunterLifecycle>();
        hunterCombat = GetComponent<HunterCombat>();
        hunterGatherController = GetComponent<HunterGatherController>();
        hunterFleeController = GetComponent<HunterFleeController>();

        visualFeedback = GetComponent<HunterVisualFeedback>();

        if (visualFeedback == null)
            visualFeedback = GetComponentInChildren<HunterVisualFeedback>(true);
    }

    private void Start()
    {
        if (pathfinder == null && NavigationGraph.Instance != null)
            pathfinder = NavigationGraph.Instance.GetComponent<AStarPathfinder>();
        if (hunterNavigation != null)
        {
            hunterNavigation.Initialize(
                pathfinder,
                searchNodes,
                huntingZone,
                speed,
                nodeReachDistance
            );

            hunterNavigation.SetObstacleMask(obstacleMask);
        }
        else
        {
            Debug.LogError("[Hunter] No se encontró HunterNavigation.");
        }

        if (hunterLifecycle != null)
        {
            hunterLifecycle.Initialize(
                this,
                hunterNavigation,
                initialZoneTransform,
                respawnTime
            );
        }
        else
        {
            Debug.LogError("[Hunter] No se encontró HunterLifecycle.");
        }

        if (hunterCombat != null)
        {
            hunterCombat.Initialize(
                hunterPerception,
                visualFeedback,
                rangeAttackRadius,
                meleeAttackRadius,
                attackDamage
            );
        }
        else
        {
            Debug.LogError("[Hunter] No se encontró HunterCombat.");
        }

        if (hunterGatherController != null)
        {
            hunterGatherController.Initialize(gatherDistance, gatherTime);
        }
        else
        {
            Debug.LogError("[Hunter] No se encontró HunterGatherController.");
        }

        if (hunterFleeController != null)
        {
            hunterFleeController.Initialize(
                pathfinder,
                safeZones,
                wolfPerceptionInterval,
                safeZoneWaitTime,
                safeZoneCenterDistance,
                escapeRecalculationInterval
            );
        }
        else
        {
            Debug.LogError("[Hunter] No se encontró HunterFleeController.");
        }

        if (initialZoneTransform != null)
            TeleportTo(initialZoneTransform.position);

        ChangeState(HunterState.Search);
        AgentStateMonitor.Instance?.RegisterAgent(gameObject,"Hunter",currentState.ToString());
    }

    private void Update()
    {
        if (currentState == HunterState.Dead ||(hunterLifecycle != null && hunterLifecycle.IsRespawning))
        {
            return;
        }

        if (rangedAttackCooldown > 0f)
            rangedAttackCooldown -= Time.deltaTime;

        if (meleeAttackCooldown > 0f)
            meleeAttackCooldown -= Time.deltaTime;

        UpdateInterestSpawning();

        CheckAndTriggerFlee();

        ExecuteStateBehavior();
    }

    public void ChangeState(HunterState newState)
    {
        if (currentState == newState)
            return;

        currentState = newState;
        AgentStateMonitor.Instance?.UpdateAgentState(gameObject,newState.ToString());

        if (hunterNavigation != null)
            hunterNavigation.ClearPath();

        switch (newState)
        {
            case HunterState.Search:
                lostTargetTimer = 0f;
                break;

            case HunterState.Attack:
                lostTargetTimer = 0f;
                break;

            case HunterState.Gather:

                if (hunterGatherController != null)
                    hunterGatherController.ResetGather();

                break;

            case HunterState.Flee:
                currentTarget = null;

                if (hunterFleeController != null)
                    hunterFleeController.ResetFlee();

                break;

            case HunterState.Dead:
                currentTarget = null;
                StopMovement();

                if (hunterLifecycle != null)
                    hunterLifecycle.StartRespawn();

                return;
        
        }

        UpdateVisualFeedback();
    }

    private void UpdateVisualFeedback()
    {
        if (visualFeedback == null)
            return;

        switch (currentState)
        {
            case HunterState.Search:
            case HunterState.Exit:
                visualFeedback.ShowNormal();
                break;

            case HunterState.Attack:
                visualFeedback.ShowAttack();
                break;

            case HunterState.Gather:
                visualFeedback.ShowGather();
                break;

            case HunterState.Flee:
                visualFeedback.ShowFlee();
                break;
        }
    }

    private void ExecuteStateBehavior()
    {
        switch (currentState)
        {
            case HunterState.Search:
                UpdateSearch();
                break;

            case HunterState.Attack:
                UpdateAttack();
                break;

            case HunterState.Gather:
                UpdateGather();
                break;

            case HunterState.Flee:
                UpdateFlee();
                break;

            case HunterState.Exit:
                UpdateExit();
                break;
        }
    }

    private void UpdateSearch()
    {
        if (currentGathers >= requiredGathers)
        {
            ChangeState(HunterState.Exit);
            return;
        }

        perceptionTimer += Time.deltaTime;

        if (perceptionTimer >= perceptionInterval)
        {
            perceptionTimer = 0f;

            Transform target =
                hunterPerception.FindBestVisibleBoid(rangedAttackCooldown);

            if (target != null)
            {
                BoidHealth health = target.GetComponentInParent<BoidHealth>();

                currentTarget = target;

                if (health != null && health.IsDead)
                {
                    ChangeState(HunterState.Gather);
                    return;
                }

                if (health != null && !health.IsDead)
                {
                    lastKnownTargetPos = target.position;
                    ChangeState(HunterState.Attack);
                    return;
                }
            }
        }

        if (hunterNavigation == null)
            return;

        hunterNavigation.NavigateSearch();
    }

    private void UpdateAttack()
    {
        if (!IsValidTarget(currentTarget))
        {
            ReturnToSearch();
            return;
        }

        if (hunterNavigation == null || hunterCombat == null)
            return;

        BoidHealth health = currentTarget.GetComponentInParent<BoidHealth>();

        if (health == null)
        {
            ReturnToSearch();
            return;
        }

        if (health.IsDead)
        {
            ChangeState(HunterState.Gather);
            return;
        }

        float distance = GetFlatDistance(transform.position, currentTarget.position);
        bool hasLOS = hunterPerception.HasLineOfSight(currentTarget);

        if (distance > hunterPerception.PerceptionRadius || !hasLOS)
        {
            lostTargetTimer += Time.deltaTime;

            if (lostTargetTimer >= lostTargetTolerance)
            {
                ReturnToSearch();
                return;
            }

            hunterNavigation.NavigateToPosition(lastKnownTargetPos);
            return;
        }

        lastKnownTargetPos = currentTarget.position;
        lostTargetTimer = 0f;

        if (distance <= meleeAttackRadius)
        {
            hunterNavigation.StopMovement();
            hunterNavigation.RotateTowards(currentTarget.position);

            if (meleeAttackCooldown <= 0f)
            {
                if (hunterCombat.TryMeleeAttack(currentTarget))
                {
                    meleeAttackCooldown = tba;
                    ReturnToSearch();
                }
            }

            return;
        }

        if (distance <= rangeAttackRadius)
        {
            hunterNavigation.StopMovement();
            hunterNavigation.RotateTowards(currentTarget.position);

            if (rangedAttackCooldown <= 0f)
            {
                if (hunterCombat.TryRangedAttack(currentTarget))
                {
                    rangedAttackCooldown = tba;
                    ReturnToSearch();
                }
            }

            return;
        }

        Vector3 predictedPosition = hunterPerception.GetPursuitPosition(
            currentTarget,
            speed,
            pursuitPredictionMultiplier,
            maxPursuitPredictionTime
        );

        hunterNavigation.MoveTowards(predictedPosition);
    }

    private void UpdateGather()
    {
        if (hunterGatherController == null)
            return;

        HunterGatherController.GatherResult result =
            hunterGatherController.UpdateGather(currentTarget);

        switch (result)
        {
            case HunterGatherController.GatherResult.Invalid:
                ReturnToSearch();
                break;

            case HunterGatherController.GatherResult.Completed:
                CompleteGather();

                currentTarget = null;

                if (currentGathers >= requiredGathers)
                    ChangeState(HunterState.Exit);
                else
                    ChangeState(HunterState.Search);
                break;
        }
    }

    private void CompleteGather()
    {
        currentGathers++;

        Debug.Log(
            $"[Hunter] Recolección completada: {currentGathers}/{requiredGathers}"
        );
    }

    private void CheckAndTriggerFlee()
    {

        if (currentState == HunterState.Flee)
            return;

        if (hunterFleeController != null &&
            hunterFleeController.CheckForWolf())
        {
            Debug.Log("[Hunter] ¡Lobo detectado! Entrando en Flee.");
            ChangeState(HunterState.Flee);
        }
    }

    private void UpdateFlee()
    {
        if (hunterFleeController == null)
            return;

        HunterFleeController.FleeResult result =
            hunterFleeController.UpdateFlee();

        if (result == HunterFleeController.FleeResult.SafeZoneCompleted)
        {
            if (currentGathers >= requiredGathers)
                ChangeState(HunterState.Exit);
            else
                ChangeState(HunterState.Search);
        }
    }

    private void UpdateExit()
    {
        if (exitZone == null)
        {
            Debug.LogError("[Hunter] No hay Exit Zone asignada.");
            StopMovement();
            return;
        }

        if (hunterNavigation == null || hunterLifecycle == null)
            return;

        bool reachedExit = hunterNavigation.NavigateToExit(exitZone);

        if (reachedExit && !hunterLifecycle.IsRespawning)
            hunterLifecycle.StartExitReturn();
    }

    public void ResetAfterLifecycle()
    {
        currentGathers = 0;

        ResetRuntimeData();

        if (hunterNavigation != null)
        {
            hunterNavigation.ResetNavigation(
                hunterNavigation.GetNearestSearchNodeIndex()
            );
        }
    }

    public void FinishLifecycle()
    {
        ChangeState(HunterState.Search);
    }

    private bool IsValidTarget(Transform target)
    {
        return target != null && target.gameObject.activeInHierarchy;
    }

    private void StopMovement()
    {
        if (hunterNavigation != null)
            hunterNavigation.StopMovement();
    }

    private void TeleportTo(Vector3 position)
    {
        if (hunterNavigation != null)
            hunterNavigation.TeleportTo(position);
        else
            transform.position = position;
    }

    public void Die()
    {
        if (currentState == HunterState.Dead)
            return;

        ChangeState(HunterState.Dead);
    }

    public void KillHunter()
    {
        Die();
    }

    public void TakeDamage(int damage)
    {
        Die();
    }

    private void ResetRuntimeData()
    {
        if (hunterNavigation != null)
            hunterNavigation.ClearPath();

        if (hunterPerception != null)
            hunterPerception.ResetPerception();

        if (hunterGatherController != null)
            hunterGatherController.ResetGather();

        if (hunterFleeController != null)
            hunterFleeController.ResetFlee();

        currentTarget = null;

        rangedAttackCooldown = 0f;
        meleeAttackCooldown = 0f;
        perceptionTimer = 0f;
        lostTargetTimer = 0f;

        interestSpawnTimer = 0f;

        StopMovement();
    }

    private void UpdateInterestSpawning()
    {
        InterestManager manager = InterestManager.Instance;

        interestSpawnTimer += Time.deltaTime;

        if (interestSpawnTimer < interestSpawnInterval)
            return;

        interestSpawnTimer = 0f;

        manager.SpawnInterest();
    }

    private void ReturnToSearch()
    {
        currentTarget = null;
        lostTargetTimer = 0f;

        ChangeState(HunterState.Search);
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, rangeAttackRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, gatherDistance);

    }
    private void OnDestroy()
    {
        AgentStateMonitor.Instance?.UnregisterAgent(gameObject);
    }
}
