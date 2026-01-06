using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

public class UnitController : NetworkBehaviour
{
    public enum UnitType
    {
        Pawn, // Melee foot soldier
        Bishop, // Battle mage, long atk cd, high aoe damage around target
        Knight, // Tank, basic tank, fast, low damage high hp
        Rook, // Tank, does not move, 3 hex block
        Queen, // Solar Mage, does not move, meteors from above
        King // Does not move, high hp, when 0, game ends. 
    }

    [Header("Stats")]
    public float maxHP = 100f;
    public float attackDamage = 10f;
    public float attackRange = 1.8f;
    public float attackCooldown = 1.2f;
    public int teamId = 0;
    public int unitTypeId;
    public float detectionRadius = 50f;
    public bool canMove = true;
    public UnitType unitType;

    [Header("Bishop Specific")]
    public float aoeRadius = 2f;
    public float aoeDamage = 20f;

    [Header("Queen Specific")]
    public int maxTargets = 3;

    [HideInInspector]
    public BoardSlot CurrentSlot;

    [Header("Team Materials")]
    [SerializeField] private Material teamWhiteMaterial;
    [SerializeField] private Material teamBlackMaterial;

    [Header("Misc Params")]
    public int fusionCount = 0;

    public float currentHP;
    private float attackTimer;

    private NavMeshAgent agent;
    private UnitController currentTarget;

    private float visualYOffset;

    [HideInInspector]
    public ulong SourceUnitNetworkId;

    public float GetHP() => currentHP;

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

    void Start()
    {
        ApplyTeamMaterial();
    }

    void Update()
    {
        if (!IsServer) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value
            != GamePhaseManager.GamePhase.Battle)
            return;

        if (canMove)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh)
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

        if (canMove && agent != null)
        {
            if (distance > attackRange)
            {
                agent.isStopped = false;
                agent.SetDestination(currentTarget.transform.position);
            }
            else
            {
                agent.isStopped = true;
            }
        }

        if (distance <= attackRange)
        {
            TryAttack();
        }
    }

    public void SetHP(float hp)
    {
        if (hp > 0)
        {
            currentHP = hp;
        }
        else
        {
            Die();
        }
    }

    public void AddHP(float hp)
    {
        SetHP(currentHP + hp);
    }

    void ApplyTeamMaterial()
    {
        if (!IsSpawned)
            return;

        bool isServerOwned =
            OwnerClientId == NetworkManager.ServerClientId;

        Material mat = isServerOwned
            ? teamWhiteMaterial
            : teamBlackMaterial;

        if (mat == null)
            return;

        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
            return;

        foreach (var renderer in renderers)
        {
            renderer.material = mat;
        }
    }

    void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            detectionRadius
        );

        float closest = float.MaxValue;
        UnitController best = null;

        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null)
                continue;

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

        if(unitType == UnitType.Queen)
        {
            ApplyMultiTargetAttack();
        }
        else
        {
            currentTarget.TakeDamage(attackDamage);

            if (unitType == UnitType.Bishop)
            {
                Vector3 from = transform.position + Vector3.up;
                Vector3 to = currentTarget.transform.position;

                PlayAttackVFXClientRpc(
                    AttackVFXType.Bishop_Fireball,
                    from,
                    to
                );

                ApplyAOEDamage(to);

                PlayAttackVFXClientRpc(
                    AttackVFXType.Bishop_AOE_Impact,
                    Vector3.zero,
                    to
                );
            }
        }
    }

    public void TakeDamage(float dmg)
    {
        currentHP -= dmg;
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    public void ApplyAOEDamage(Vector3 center)
    {
        Collider[] hits = Physics.OverlapSphere(center, aoeRadius);

        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null)
                continue;

            if (unit.teamId == teamId)
                continue;

            if (unit == currentTarget)
                continue;

            unit.TakeDamage(aoeDamage);
        }
    }

    void ApplyMultiTargetAttack()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange
        );

        var enemies = new List<UnitController>();

        foreach (var hit in hits)
        {
            UnitController unit = hit.GetComponent<UnitController>();
            if (unit == null)
                continue;

            if (unit.teamId == teamId || unit.IsDead())
                continue;

            enemies.Add(unit);
        }

        enemies.Sort((a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(
                Vector3.Distance(transform.position, b.transform.position)
            )
        );

        int count = Mathf.Min(maxTargets, enemies.Count);

        for (int i = 0; i < count; i++)
        {
            enemies[i].TakeDamage(attackDamage);
        }
    }

    public bool IsDead()
    {
        return currentHP <= 0f;
    }

    void Die()
    {
        if (!IsServer) return;

        if (unitType == UnitType.King)
        {
            GamePhaseManager.Instance.OnKingKilled(teamId);
            GetComponent<NetworkObject>().Despawn(true);
            return;
        }

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

    public void ReturnToSlot()
    {
        if (!IsServer || CurrentSlot == null)
            return;

        Collider unitCol = GetComponent<Collider>();
        Collider slotCol = CurrentSlot.GetComponent<Collider>();

        float yOffset = unitCol.bounds.extents.y + slotCol.bounds.extents.y;

        Vector3 pos = CurrentSlot.SnapPosition;
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
        if (IsServer && CurrentSlot != null)
        {
            CurrentSlot.Clear();
            CurrentSlot = null;
        }

        if (GamePhaseManager.Instance != null)
        {
            GamePhaseManager.Instance.CurrentPhase.OnValueChanged -= OnPhaseChanged;
        }
    }


    void OnPhaseChanged(
        GamePhaseManager.GamePhase oldPhase,
        GamePhaseManager.GamePhase newPhase)
    {
        if (!IsServer) return;

        if (!canMove || agent == null)
        {
            SnapToNavMesh();
            return;
        } 

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

    [ClientRpc]
    void PlayAttackVFXClientRpc(
        AttackVFXType type,
        Vector3 from,
        Vector3 to
    )
    {
        if (VFXManager.Instance == null)
            return;

        VFXManager.Instance.Play(type, from, to);
    }
}
