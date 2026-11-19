
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class NavigationZone : MonoBehaviour
{
    [Header("Configuración de la zona")]
    [SerializeField] private bool blocksWolves = true;

    private BoxCollider zoneCollider;

    public bool BlocksWolves => blocksWolves;

    private void Awake()
    {
        zoneCollider = GetComponent<BoxCollider>();
    }

    public bool ContainsPositionXZ(Vector3 position)
    {
        if (zoneCollider == null)
            zoneCollider = GetComponent<BoxCollider>();

        if (zoneCollider == null)
            return false;

        Vector3 localPosition =
            zoneCollider.transform.InverseTransformPoint(position);

        Vector3 center = zoneCollider.center;
        Vector3 halfSize = zoneCollider.size * 0.5f;

        return localPosition.x >= center.x - halfSize.x &&
               localPosition.x <= center.x + halfSize.x &&
               localPosition.z >= center.z - halfSize.z &&
               localPosition.z <= center.z + halfSize.z;
    }
}

