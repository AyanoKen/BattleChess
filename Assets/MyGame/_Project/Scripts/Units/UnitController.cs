using UnityEngine;
using UnityEngine.AI;

public class UnitController : MonoBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public int teamId = 0;

    private float currentHP;
    private float attackTimer;

    private NavMeshAgent agent;
    private UnitController currentTarget;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;
    }

    void Update()
    {
        if (currentTarget == null || currentTarget.IsDead())
        {
            FindTarget();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            currentTarget.transform.position
        );

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.transform.position);
        }
        else
        {
            agent.isStopped = true;
            TryAttack();
        }
    }

    void FindTarget()
    {
        UnitController[] allUnits = FindObjectsOfType<UnitController>();
        float closest = float.MaxValue;
        UnitController best = null;

        foreach (var unit in allUnits)
        {
            if (unit == this || unit.IsDead() || unit.teamId == teamId)
            {
                continue;
            }

            float dist = Vector3.Distance(
                transform.position,
                unit.transform.position
            );

            if (dist < closest)
            {
                closest = dist;
                best = unit;
            }
        }

        currentTarget = best;
    }

    void TryAttack()
    {
        attackTimer -= Time.deltaTime;
        if (attackTimer > 0f) return;

        attackTimer = attackCooldown;
        currentTarget.TakeDamage(attackDamage);
    }

    public void TakeDamage(float dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public bool IsDead()
    {
        return currentHP <= 0f;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
