
using UnityEngine;

public class WolfVisualFeedback : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("State Colors")]
    [SerializeField] private Color patrolColor = Color.green;
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color searchColor = Color.yellow;

    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderers == null ||
            targetRenderers.Length == 0)
        {
            targetRenderers =
                GetComponentsInChildren<Renderer>();
        }
    }

    public void SetState(WolfState state)
    {
        Color stateColor;

        switch (state)
        {
            case WolfState.Patrol:
                stateColor = patrolColor;
                break;

            case WolfState.Chase:
                stateColor = chaseColor;
                break;

            case WolfState.SearchLastKnown:
                stateColor = searchColor;
                break;

            default:
                return;
        }

        ApplyColor(stateColor);
    }

    private void ApplyColor(Color color)
    {
        foreach (Renderer targetRenderer in targetRenderers)
        {
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);

            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}

