
using System.Collections;
using UnityEngine;

public class HunterVisualFeedback : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color attackColor = Color.green;
    [SerializeField] private Color rangedAttackColor = Color.blue;
    [SerializeField] private Color gatherColor = Color.yellow;
    [SerializeField] private Color fleeColor = Color.cyan;

    [Header("Attack Effects")]
    [SerializeField] private float meleeFlashDuration = 0.3f;
    [SerializeField] private float rangedFlashDuration = 0.5f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine attackCoroutine;

    private static readonly int BaseColorID =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorID =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        Initialize();
        SetColor(normalColor);
    }

    private void Initialize()
    {
        if (propertyBlock != null)
            return;

        renderers = GetComponentsInChildren<Renderer>(true);
        propertyBlock = new MaterialPropertyBlock();
    }

    public void ShowAttack()
    {
        ShowMeleeAttack();
    }

    public void ShowMeleeAttack()
    {
        PlayAttackFlash(attackColor, meleeFlashDuration);
    }

    public void ShowRangedAttack()
    {
        PlayAttackFlash(rangedAttackColor, rangedFlashDuration);
    }

    private void PlayAttackFlash(Color color, float duration)
    {
        Initialize();

        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(AttackFlash(color, duration));
    }

    private IEnumerator AttackFlash(Color color, float duration)
    {
        SetColor(color);

        yield return new WaitForSeconds(duration);

        SetColor(normalColor);
        attackCoroutine = null;
    }

    public void ShowGather()
    {
        Initialize();
        StopAttackEffect();
        SetColor(gatherColor);
    }

    public void ShowNormal()
    {
        Initialize();

        if (attackCoroutine != null)
            return;

        SetColor(normalColor);
    }

    public void ShowFlee()
    {
        Initialize();
        StopAttackEffect();
        SetColor(fleeColor);
    }

    private void StopAttackEffect()
    {
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = null;
    }

    private void SetColor(Color color)
    {
        Initialize();

        foreach (Renderer currentRenderer in renderers)
        {
            if (currentRenderer == null)
                continue;

            currentRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorID, color);
            propertyBlock.SetColor(ColorID, color);
            currentRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
