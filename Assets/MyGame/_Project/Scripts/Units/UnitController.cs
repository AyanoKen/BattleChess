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
    public int unitTypeId;

    [HideInInspector]
    public BoardSlot CurrentSlot;

    private float currentHP;
    private float attackTimer;

    private NavMeshAgent agent;
    private UnitController currentTarget;

    private float visualYOffset;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentHP = maxHP;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            visualYOffset = col.bounds.extents.y;
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value
            != GamePhaseManager.GamePhase.Battle)
            return;

        if (!agent.enabled || !agent.isOnNavMesh)
            return;

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
                continue;

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

    // ---------- Placement ----------

    public float GetPlacementYOffset(BoardSlot slot)
    {
        Collider unitCol = GetComponent<Collider>();
        Collider slotCol = slot.GetComponent<Collider>();

        if (unitCol == null || slotCol == null)
            return 0f;

        return unitCol.bounds.extents.y + slotCol.bounds.extents.y;
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

    // ---------- NavMesh ----------

    void SnapToNavMesh()
    {
        if (NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            2.5f,
            NavMesh.AllAreas))
        {
            Vector3 snapped = hit.position;
            snapped.y += visualYOffset;
            transform.position = snapped;
        }
        else
        {
            Debug.LogWarning($"{name} failed to snap to NavMesh");
        }
    }

    // ---------- Phase Handling ----------

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        GamePhaseManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;

        OnPhaseChanged(
            GamePhaseManager.GamePhase.Prep,
            GamePhaseManager.Instance.CurrentPhase.Value
        );
    }

    public override void OnNetworkDespawn()
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
        {
            agent.enabled = true;

            SnapToNavMesh();

            if (!agent.isOnNavMesh)
            {
                Debug.LogError($"{name} is not on NavMesh after snap");
                agent.enabled = false;
            }
        }
        else
        {
            agent.enabled = false;
        }
    }
}
