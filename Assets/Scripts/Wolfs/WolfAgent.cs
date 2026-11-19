using UnityEngine;

public enum WolfState
{
    Patrol,
    Chase,
    SearchLastKnown
}

public class WolfAgent : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private GraphNode[] patrolNodes;
    [SerializeField] private float nodeReachDistance = 0.5f;
    [SerializeField] private float captureRadius = 1.5f;

    private int patrolNodeIndex = 0;

    [Header("Perception (FOV / Line of Sight)")]
    [SerializeField] private float viewRadius = 10f;
    [SerializeField] private float viewAngle = 90f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LayerMask hunterMask;
    [SerializeField] private float perceptionInterval = 0.15f;

    private float perceptionTimer;

    private WolfPerception wolfPerception;
    private bool hunterVisible;
    private bool lossOfSightReported;

    [Header("Navigation")]
    [SerializeField] private AStarPathfinder pathfinder;
    [SerializeField] private float movementSpeed = 3f;
    [SerializeField] private float pathRecalculationInterval = 0.4f;
    [SerializeField] private float destinationRecalculationThreshold = 2.5f;

    private WolfNavigation wolfNavigation;
    private WolfCaptureController wolfCaptureController;
    private WolfVisualFeedback wolfVisualFeedback;

    [Header("Persecución")]
    [SerializeField] private float lostSightGraceTime = 1.5f;

    private float lostSightTimer;

    [Header("Search Config")]
    [SerializeField] private float maxSearchTime = 5f;

    private float searchTimer;

    private WolfState currentState;
    private HunterAgent targetHunter;
    private Vector3 lastKnownHunterPosition;

    private bool hasAlertedPack;

    private void Start()
    {
        if (pathfinder == null)
            pathfinder = AStarPathfinder.Instance;

        if (pathfinder == null)
        {
            Debug.LogError(
                $"[WolfAgent] No se encontró ningún AStarPathfinder en la escena para {gameObject.name}"
            );
        }

        wolfPerception = GetComponent<WolfPerception>();
        wolfNavigation = GetComponent<WolfNavigation>();
        wolfCaptureController = GetComponent<WolfCaptureController>();
        wolfVisualFeedback = GetComponent<WolfVisualFeedback>();

        if (wolfPerception == null)
        {
            Debug.LogError(
                $"[WolfAgent] Falta WolfPerception en {gameObject.name}"
            );

            enabled = false;
            return;
        }

        if (wolfNavigation == null)
        {
            Debug.LogError(
                $"[WolfAgent] Falta WolfNavigation en {gameObject.name}"
            );

            enabled = false;
            return;
        }

        if (wolfCaptureController == null)
        {
            Debug.LogError(
                $"[WolfAgent] Falta WolfCaptureController en {gameObject.name}"
            );

            enabled = false;
            return;
        }

        wolfPerception.Initialize(
            viewRadius,
            viewAngle,
            obstacleMask,
            hunterMask
        );

        wolfNavigation.Initialize(
            pathfinder,
            movementSpeed,
            nodeReachDistance,
            pathRecalculationInterval,
            destinationRecalculationThreshold
        );

        if (WolfManager.Instance != null)
        {
            WolfManager.Instance.RegisterWolf(this);

            Debug.Log(
                $"[Wolf] {gameObject.name} registrado correctamente."
            );
        }
        else
        {
            Debug.LogError(
                "[Wolf] No se encontró WolfManager en la escena."
            );
        }

        ChangeState(WolfState.Patrol);
        AgentStateMonitor.Instance?.RegisterAgent(gameObject,"Wolf",currentState.ToString());

        if (wolfVisualFeedback != null)
            wolfVisualFeedback.SetState(currentState);
    }

    private void OnDestroy()
    {
        if (WolfManager.Instance != null)
            WolfManager.Instance.UnregisterWolf(this);
        AgentStateMonitor.Instance?.UnregisterAgent(gameObject);
    }

    private void Update()
    {
        perceptionTimer += Time.deltaTime;

        if (perceptionTimer >= perceptionInterval)
        {
            perceptionTimer = 0f;

            HunterAgent spottedHunter =
                wolfPerception.CheckForHunter();

            hunterVisible = spottedHunter != null;

            if (hunterVisible)
            {
                bool newlyDetected =
                    currentState != WolfState.Chase ||
                    targetHunter != spottedHunter;

                targetHunter = spottedHunter;

                lastKnownHunterPosition =
                    spottedHunter.transform.position;

                lostSightTimer = 0f;
                lossOfSightReported = false;

                if (newlyDetected)
                {
                    Debug.Log(
                        $"[Wolf] {gameObject.name} detectó al Hunter en " +
                        $"{lastKnownHunterPosition}"
                    );
                }

                if (!hasAlertedPack)
                {
                    Debug.Log(
                        $"[Wolf] {gameObject.name} emitió un aullido de alerta."
                    );

                    WolfManager.Instance?.BroadcastHowlAlert(
                        this,
                        lastKnownHunterPosition
                    );

                    hasAlertedPack = true;
                }

                if (currentState != WolfState.Chase)
                    ChangeState(WolfState.Chase);
            }
        }

        if (currentState == WolfState.Chase && !hunterVisible)
        {
            if (!lossOfSightReported)
            {
                Debug.Log(
                    $"[Wolf] {gameObject.name}: perdió de vista al Hunter. " +
                    $"Última posición conocida: {lastKnownHunterPosition}"
                );

                lossOfSightReported = true;
            }

            lostSightTimer += Time.deltaTime;

            if (lostSightTimer >= lostSightGraceTime)
            {
                targetHunter = null;
                hasAlertedPack = false;

                ChangeState(WolfState.SearchLastKnown);
            }
        }

        switch (currentState)
        {
            case WolfState.Patrol:
                UpdatePatrol();
                break;

            case WolfState.Chase:
                UpdateChase();
                break;

            case WolfState.SearchLastKnown:
                UpdateSearchLastKnown();
                break;
        }
    }

    public void ReceiveHowlAlert(Vector3 alertPosition)
    {
        if (currentState == WolfState.Patrol ||
            currentState == WolfState.SearchLastKnown)
        {
            lastKnownHunterPosition = alertPosition;

            Debug.Log(
                $"[Wolf] {gameObject.name} recibió una alerta. " +
                $"Última posición conocida: {alertPosition}"
            );

            ChangeState(WolfState.SearchLastKnown);
        }
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    private void ChangeState(WolfState newState)
    {
        if (currentState == newState)
            return;

        WolfState previousState = currentState;
        currentState = newState;

        AgentStateMonitor.Instance?.UpdateAgentState(gameObject,newState.ToString());

        wolfNavigation.ResetNavigation();

        searchTimer = 0f;

        if (wolfVisualFeedback != null)
            wolfVisualFeedback.SetState(newState);

        Debug.Log(
            $"[Wolf] {gameObject.name}: {previousState} -> {newState}"
        );
    }

    private void UpdatePatrol()
    {
        if (patrolNodes == null || patrolNodes.Length == 0)
        {
            Debug.LogWarning(
                $"[Wolf] {gameObject.name} no tiene nodos de patrulla."
            );

            return;
        }

        GraphNode destinationNode = patrolNodes[patrolNodeIndex];

        if (destinationNode == null)
            return;

        if (!NavigationGraph.Instance.IsNodeAllowedForWolves(destinationNode))
        {
            patrolNodeIndex = (patrolNodeIndex + 1) % patrolNodes.Length;

            wolfNavigation.ResetNavigation();

            return;
        }

        float distance = GetFlatDistance(
            transform.position,
            destinationNode.transform.position
        );

        if (distance <= nodeReachDistance ||
            wolfNavigation.HasCompletedPath())
        {
            patrolNodeIndex = (patrolNodeIndex + 1) % patrolNodes.Length;

            wolfNavigation.ResetNavigation();

            return;
        }

        wolfNavigation.MoveAlongPath(
            destinationNode.transform.position
        );
    }

    private void UpdateChase()
    {
        if (targetHunter == null)
        {
            ChangeState(WolfState.SearchLastKnown);
            return;
        }

        if (!NavigationGraph.Instance.IsPositionAllowedForWolves(
            targetHunter.transform.position))
        {
            targetHunter = null;
            hasAlertedPack = false;

            ChangeState(WolfState.Patrol);

            wolfNavigation.StopMovement();

            Debug.Log(
                $"[Wolf] {gameObject.name}: Hunter entró en una zona protegida."
            );

            return;
        }

        lastKnownHunterPosition = targetHunter.transform.position;

        float distance = GetFlatDistance(
            transform.position,
            targetHunter.transform.position
        );

        if (distance <= captureRadius)
        {
            CaptureHunter(targetHunter);
            return;
        }

        wolfNavigation.MoveAlongPath(
            targetHunter.transform.position
        );
    }

    private void UpdateSearchLastKnown()
    {
        wolfNavigation.MoveAlongPath(lastKnownHunterPosition);

        bool finishedSearching = wolfNavigation.HasFinishedPath();

        if (!finishedSearching)
        {
            searchTimer = 0f;
            return;
        }

        wolfNavigation.StopMovement();

        searchTimer += Time.deltaTime;

        if (searchTimer >= maxSearchTime)
        {
            searchTimer = 0f;
            ChangeState(WolfState.Patrol);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (currentState != WolfState.Chase ||
            targetHunter == null)
        {
            return;
        }

        HunterAgent hitHunter =
            hit.collider.GetComponentInParent<HunterAgent>();

        if (hitHunter == null)
            return;

        CaptureHunter(hitHunter);
    }

    private void CaptureHunter(HunterAgent hunter)
    {
        if (hunter == null)
            return;

        wolfCaptureController.CaptureHunter(hunter);

        targetHunter = null;
        hasAlertedPack = false;

        ChangeState(WolfState.Patrol);

        Debug.Log(
            $"[Wolf] {gameObject.name} regresó a Patrol."
        );
    }
}
