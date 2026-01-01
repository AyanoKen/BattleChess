using Unity.Netcode;
using UnityEngine;
using System.Linq;


public class GamePhaseManager : NetworkBehaviour
{
    public static GamePhaseManager Instance;

    public enum GamePhase
    {
        Prep,
        Battle,
        Resolution
    }

    [SerializeField] private GameObject[] battleUnitPrefabs;

    [SerializeField] private float prepDuration = 20f;
    [SerializeField] private float battleDuration = 10f;

    [SerializeField] private float knightHpOffset = 0f; //TODO: DECIDE THESE IN EDITOR
    [SerializeField] private float bishopHpOffset = 0f;

    public NetworkVariable<GamePhase> CurrentPhase =
        new NetworkVariable<GamePhase>(
            GamePhase.Prep,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<float> PhaseTimer =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartPrepPhase();
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (PhaseTimer.Value > 0f)
        {
            PhaseTimer.Value -= Time.deltaTime;

            if (PhaseTimer.Value <= 0f)
            {
                AdvancePhase();
            }
        }
    }

    void AdvancePhase()
    {
        switch (CurrentPhase.Value)
        {
            case GamePhase.Prep:
                StartBattlePhase();
                break;

            case GamePhase.Battle:
                StartResolutionPhase();
                break;
        }
    }

    void StartPrepPhase()
    {
        CurrentPhase.Value = GamePhase.Prep;
        PhaseTimer.Value = prepDuration;
        Debug.Log("Prep Phase");
    }

    void StartBattlePhase()
    {
        CurrentPhase.Value = GamePhase.Battle;
        PhaseTimer.Value = battleDuration;
        Debug.Log("Battle Phase");

        SpawnEnemyUnitsOnBoards();
    }

    void StartResolutionPhase()
    {
        CurrentPhase.Value = GamePhase.Resolution;
        PhaseTimer.Value = 0f;
        Debug.Log("Resolving Battle");

        ResolveBattle();
    }

    void ResolveBattle() //TODO: Check for a small sync issue
    {
        foreach (var unit in FindObjectsOfType<UnitController>())
        {
            if (unit.SourceUnitNetworkId != 0)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                    .TryGetValue(unit.SourceUnitNetworkId, out var realObj))
                {
                    var realUnit = realObj.GetComponent<UnitController>();
                    realUnit.SetHP(unit.GetHP());
                }

                unit.GetComponent<NetworkObject>().Despawn(true);
            }
        }

        RestoreHostUnits();
        StartPrepPhase();
    }

    void RestoreHostUnits()
    {
        foreach (var unit in FindObjectsOfType<UnitController>())
        {
            if (unit.CurrentSlot == null)
                continue;

            unit.ReturnToSlot();
        }
    }


    void SpawnEnemyUnitsOnBoards()
    {
        BoardManager bm = FindObjectOfType<BoardManager>();

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            PlayerBoard playerBoard = bm.GetBoardForClient(clientId);
            if (playerBoard == null)
                continue;

            ulong enemyClientId =
                NetworkManager.Singleton.ConnectedClientsIds
                    .First(id => id != clientId);

            PlayerBoard enemyBoard =
                bm.GetBoardForClient(enemyClientId);

            if (enemyBoard == null)
                continue;

            var enemyState = enemyBoard.CaptureBoardState();

            foreach (var unitState in enemyState)
            {
                SpawnEnemyUnit(
                    unitState,
                    playerBoard,
                    enemyClientId
                );
            }
        }
    }

    void SpawnEnemyUnit(
        UnitBoardState state,
        PlayerBoard targetBoard,
        ulong enemyOwnerId)
    {
        BoardSlot spawnSlot =
            targetBoard.GetEnemySlotByIndex(state.slotIndex);

        if (spawnSlot == null)
        {
            Debug.LogWarning(
                $"No enemy slot for index {state.slotIndex}"
            );
            return;
        }

        GameObject prefab = GetBattlePrefab(state.unitTypeId);

        GameObject unit = Instantiate(
            prefab,
            spawnSlot.SnapPosition,
            Quaternion.identity
        );

        UnitController controller =
            unit.GetComponent<UnitController>();

        controller.teamId =
            enemyOwnerId == NetworkManager.ServerClientId ? 0 : 1;

        controller.SourceUnitNetworkId = state.sourceUnitId;

        unit.GetComponent<NetworkObject>().Spawn();
    }

    public GameObject GetBattlePrefab(int unitTypeId)
    {
        if (unitTypeId < 0 || unitTypeId >= battleUnitPrefabs.Length)
        {
            Debug.LogError($"Invalid unitTypeId: {unitTypeId}");
            return null;
        }

        return battleUnitPrefabs[unitTypeId];
    }

    [ClientRpc]
    public void ShowPromotionUIClientRpc(
        ulong unitId,
        ClientRpcParams rpcParams = default)
    {
        PromotionUIManager.Instance.ShowPromotionUI(unitId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitPromotionChoiceServerRpc(
        ulong unitId,
        UnitController.UnitType choice)
    {
        if (GamePhaseManager.Instance.CurrentPhase.Value
            != GamePhaseManager.GamePhase.Prep)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(unitId, out var obj))
            return;

        var unit = obj.GetComponent<UnitController>();

        if (choice == UnitController.UnitType.Rook)
            return;

        ReplaceUnit(unit, choice);
    }

    void ReplaceUnit(UnitController oldUnit, UnitController.UnitType newType)
    {
        BoardSlot slot = oldUnit.CurrentSlot;
        ulong ownerId = oldUnit.OwnerClientId;
        float hp = oldUnit.GetHP();
        int teamId = oldUnit.teamId;

        int typeId = (int)newType;
        GameObject prefab = GetBattlePrefab(typeId);

        Vector3 pos = oldUnit.transform.position;
        Quaternion rot = oldUnit.transform.rotation;

        oldUnit.GetComponent<NetworkObject>().Despawn(true);

        GameObject upgraded = Instantiate(prefab, pos, rot);
        var controller = upgraded.GetComponent<UnitController>();

        upgraded.GetComponent<NetworkObject>()
            .SpawnWithOwnership(ownerId);

        controller.unitType = newType;
        controller.fusionCount = 0;

        if (newType == UnitController.UnitType.Knight)
        {
            controller.SetHP(hp + knightHpOffset);
        }
        else if (newType == UnitController.UnitType.Bishop)
        {
            controller.SetHP(hp + bishopHpOffset);
        }
        
        controller.teamId = teamId;
        controller.SnapToSlot(slot);
    }

}
