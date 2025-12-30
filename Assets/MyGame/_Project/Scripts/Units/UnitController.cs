using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

public class UnitController : NetworkBehaviour
{
    [Header("Stats")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public int teamId = 0;

    [HideInInspector]
    public BoardSlot CurrentSlot;

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
        if(!IsServer) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value != GamePhaseManager.GamePhase.Battle)
        {
            return;
        } 

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
        if (!IsServer) return;

        GetComponent<NetworkObject>().Despawn(true);
    }

    public float GetPlacementYOffset(BoardSlot slot)
    {
        Collider unitCol = GetComponent<Collider>();
        Collider slotCol = slot.GetComponent<Collider>();

        if (unitCol == null || slotCol == null)
            return 0f;

        float unitHalfHeight = unitCol.bounds.extents.y;
        float slotHalfHeight = slotCol.bounds.extents.y;

        return unitHalfHeight + slotHalfHeight;
    }

    public void SnapToSlot(BoardSlot slot)
    {
        if (!IsServer) return;

        if (CurrentSlot != null)
            CurrentSlot.Clear();

        CurrentSlot = slot;
        slot.Assign(this);

        Collider unitCol = GetComponent<Collider>();
        Collider slotCol = slot.GetComponent<Collider>();

        float yOffset = unitCol.bounds.extents.y + slotCol.bounds.extents.y;

        Vector3 pos = slot.SnapPosition;
        pos.y += yOffset;

        transform.position = pos;
    }

    void OnEnable()
    {
        if (!IsServer) return;

        GamePhaseManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (GamePhaseManager.Instance == null) return;

        GamePhaseManager.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(
        GamePhaseManager.GamePhase oldPhase,
        GamePhaseManager.GamePhase newPhase)
    {
        if (!IsServer) return;

        if (newPhase == GamePhaseManager.GamePhase.Battle)
            agent.enabled = true;
        else
            agent.enabled = false;
    }


}
