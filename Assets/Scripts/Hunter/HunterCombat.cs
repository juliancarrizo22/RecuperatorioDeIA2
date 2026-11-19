
using UnityEngine;

public class HunterCombat : MonoBehaviour
{
    private HunterPerception hunterPerception;
    private HunterVisualFeedback visualFeedback;

    private float rangeAttackRadius;
    private float meleeAttackRadius;
    private int attackDamage;

    public void Initialize(
        HunterPerception perception,
        HunterVisualFeedback feedback,
        float rangeRadius,
        float meleeRadius,
        int damage)
    {
        hunterPerception = perception;
        visualFeedback = feedback;

        rangeAttackRadius = rangeRadius;
        meleeAttackRadius = meleeRadius;
        attackDamage = damage;
    }


    public bool TryAttack(Transform target)
    {
        Debug.Log("[HunterCombat] TryAttack fue ejecutado.");

        if (target == null || hunterPerception == null)
            return false;

        BoidHealth health =
            target.GetComponentInParent<BoidHealth>();

        if (health == null)
        {
            Debug.LogWarning("[HunterCombat] No se encontró BoidHealth.");
            return false;
        }

        if (health.IsDead)
        {
            Debug.Log("[HunterCombat] El Boid ya está muerto.");
            return false;
        }

        float distance = GetFlatDistance(
            transform.position,
            target.position
        );

        bool hasLOS = hunterPerception.HasLineOfSight(target);

        Debug.Log(
            $"[HunterCombat] Distancia: {distance:F2} | " +
            $"Rango: {rangeAttackRadius:F2} | " +
            $"Melee: {meleeAttackRadius:F2} | " +
            $"LOS: {hasLOS}"
        );

        if (distance > rangeAttackRadius)
        {
            Debug.LogWarning("[HunterCombat] Fuera del rango de ataque.");
            return false;
        }

        if (!hasLOS)
        {
            Debug.LogWarning("[HunterCombat] No hay línea de visión.");
            return false;
        }

        string attackType =
            distance <= meleeAttackRadius ? "Melee" : "Ranged";

        health.TakeDamage(attackDamage);

        if (visualFeedback != null)
        {
            if (attackType == "Melee")
                visualFeedback.ShowMeleeAttack();
            else
                visualFeedback.ShowRangedAttack();
        }

        Debug.Log(
            $"[HunterCombat] Ataque {attackType} contra {health.name}. " +
            $"Vida restante: {health.CurrentHealth}"
        );

        return true;
    }

    private float GetFlatDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(
            new Vector2(a.x, a.z),
            new Vector2(b.x, b.z)
        );
    }


    public bool TryMeleeAttack(Transform target)
    {
        return TrySpecificAttack(target, meleeAttackRadius, "Melee");
    }

    public bool TryRangedAttack(Transform target)
    {
        return TrySpecificAttack(target, rangeAttackRadius, "Ranged");
    }

    private bool TrySpecificAttack(Transform target, float attackRadius, string attackType)
    {
        if (target == null || hunterPerception == null)
            return false;

        BoidHealth health = target.GetComponentInParent<BoidHealth>();

        if (health == null || health.IsDead)
            return false;

        float distance = GetFlatDistance(transform.position, target.position);
        bool hasLOS = hunterPerception.HasLineOfSight(target);

        if (distance > attackRadius || !hasLOS)
            return false;

        health.TakeDamage(attackDamage);

        if (visualFeedback != null)
        {
            if (attackType == "Melee")
                visualFeedback.ShowMeleeAttack();
            else
                visualFeedback.ShowRangedAttack();
        }

        Debug.Log($"[HunterCombat] Ataque {attackType} contra {health.name}. Vida restante: {health.CurrentHealth}");

        return true;
    }

}

