
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(HunterNavigation))]
public class HunterLifecycle : MonoBehaviour
{
    [Header("Lifecycle Settings")]
    [SerializeField] private float exitReturnDelay = 0.5f;

    private HunterAgent hunterAgent;
    private HunterNavigation hunterNavigation;
    private Transform initialZoneTransform;
    private float respawnTime;

    private bool isRespawning;

    public bool IsRespawning => isRespawning;

    public void Initialize(
        HunterAgent agent,
        HunterNavigation navigation,
        Transform initialTransform,
        float respawnDuration)
    {
        hunterAgent = agent;
        hunterNavigation = navigation;
        initialZoneTransform = initialTransform;
        respawnTime = respawnDuration;
    }

    public void StartRespawn()
    {
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    public void StartExitReturn()
    {
        if (isRespawning)
            return;

        StartCoroutine(ExitReturnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        Debug.Log("[HunterLifecycle] Hunter muerto. Iniciando reaparición.");

        hunterNavigation.StopMovement();
        SetAgentVisible(false);

        yield return new WaitForSeconds(respawnTime);

        ReturnToInitialPosition();

        hunterAgent.ResetAfterLifecycle();
        SetAgentVisible(true);

        isRespawning = false;

        hunterAgent.FinishLifecycle();

        Debug.Log("[HunterLifecycle] Hunter reapareció correctamente.");
    }

    private IEnumerator ExitReturnRoutine()
    {
        isRespawning = true;

        Debug.Log("[HunterLifecycle] Hunter salió del coto.");

        hunterNavigation.StopMovement();
        SetAgentVisible(false);

        yield return new WaitForSeconds(exitReturnDelay);

        ReturnToInitialPosition();

        hunterAgent.ResetAfterLifecycle();
        SetAgentVisible(true);

        isRespawning = false;

        hunterAgent.FinishLifecycle();

        Debug.Log("[HunterLifecycle] Hunter regresó a la zona inicial.");
    }

    private void ReturnToInitialPosition()
    {
        if (initialZoneTransform == null)
        {
            Debug.LogError(
                "[HunterLifecycle] No se asignó Initial Zone Transform."
            );
            return;
        }

        hunterNavigation.TeleportTo(initialZoneTransform.position);
    }

    private void SetAgentVisible(bool visible)
    {
        CharacterController characterController =
            GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = visible;

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }

        Collider[] colliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (collider != null &&
                collider != characterController)
            {
                collider.enabled = visible;
            }
        }
    }
}
