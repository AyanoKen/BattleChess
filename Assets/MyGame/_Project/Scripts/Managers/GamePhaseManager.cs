using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using System.Collections;

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

    [HideInInspector]
    public float PrepDuration => prepDuration;

    [HideInInspector]
    public float BattleDuration => battleDuration;


    [SerializeField] private float knightHpOffset = 0f; //TODO: DECIDE THESE IN EDITOR
    [SerializeField] private float bishopHpOffset = 0f;

    private HashSet<ulong> simulatedSourceUnits = new HashSet<ulong>();
    private bool gameOver = false;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverImage;
    [SerializeField] private TMP_Text winText;

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

    public NetworkVariable<int> HostGold =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<int> ClientGold =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    NetworkVariable<int> GetGoldVar(ulong clientId)
    {
        return clientId == NetworkManager.ServerClientId
            ? HostGold
            : ClientGold;
    }

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            HostGold.Value = 3;
            ClientGold.Value = 3;

            StartPrepPhase();
        }
    }

    void Update()
    {
        if (!IsServer || gameOver) return;

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

        simulatedSourceUnits.Clear();

        SpawnEnemyUnitsOnBoards();
    }

    void StartResolutionPhase()
    {
        CurrentPhase.Value = GamePhase.Resolution;
        PhaseTimer.Value = 0f;
        Debug.Log("Resolving Battle");

        GrantGoldToPlayers();

        ResolveBattle();
    }

    void GrantGoldToPlayers()
    {
        GrantGold(NetworkManager.ServerClientId);

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
                continue;

            GrantGold(clientId);
        }
    }

    void GrantGold(ulong clientId)
    {
        var goldVar = GetGoldVar(clientId);

        int currentGold = goldVar.Value;
        int interest = currentGold / 5;
        int gained = 1 + interest;

        goldVar.Value += gained;

        Debug.Log($"Client {clientId} gained {gained} gold. Total: {goldVar.Value}");
    }

    void ResolveBattle()
    {
        HashSet<ulong> survivedSources = new HashSet<ulong>();

        foreach (var unit in FindObjectsOfType<UnitController>())
        {
            if (unit.SourceUnitNetworkId != 0)
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                    .TryGetValue(unit.SourceUnitNetworkId, out var realObj))
                {
                    var realUnit = realObj.GetComponent<UnitController>();
                    realUnit.SetHP(unit.GetHP());
                    survivedSources.Add(unit.SourceUnitNetworkId);
                }

                unit.GetComponent<NetworkObject>().Despawn(true);
            }
        }

        foreach (ulong sourceId in simulatedSourceUnits)
        {
            if (!survivedSources.Contains(sourceId))
            {
                if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
                    .TryGetValue(sourceId, out var realObj))
                {
                    var realUnit = realObj.GetComponent<UnitController>();
                    realUnit.SetHP(-1);
                }
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

        ulong hostClientId = NetworkManager.ServerClientId;
        PlayerBoard hostBoard = bm.GetBoardForClient(hostClientId);

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (clientId == hostClientId)
                continue; // skip host

            PlayerBoard enemyBoard = bm.GetBoardForClient(clientId);
            if (enemyBoard == null)
                continue;

            var enemyState = enemyBoard.CaptureBoardState();

            foreach (var unitState in enemyState)
            {
                SpawnEnemyUnit(
                    unitState,
                    hostBoard,
                    clientId
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

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(state.sourceUnitId, out var realObj))
        {
            var realUnit = realObj.GetComponent<UnitController>();
            controller.SetHP(realUnit.GetHP());
        }

        simulatedSourceUnits.Add(controller.SourceUnitNetworkId);

        unit.GetComponent<NetworkObject>()
            .SpawnWithOwnership(enemyOwnerId);
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

    public void OnKingKilled(int deadTeamId)
    {
        if(!IsServer || gameOver) return;

        gameOver = true;

        int winningTeamId = deadTeamId == 0 ? 1 : 0;

        Debug.Log($"GAME OVER — Team {winningTeamId} wins (King of team {deadTeamId} died)");

        EndGame(winningTeamId);
    }

    void EndGame(int winningTeamId)
    {
        CurrentPhase.Value = GamePhase.Resolution;
        PhaseTimer.Value = 0f;

        ShowParticlesClientRpc(winningTeamId);

        StartCoroutine(ShowGameOverUIDelayed(winningTeamId));

        Debug.Log("Done");
    }

    [ClientRpc]
    void ShowParticlesClientRpc(int winningTeamId)
    {
        Color endColor = winningTeamId == 0 ? Color.white : Color.black;

        foreach (var board in FindObjectsOfType<PlayerBoard>())
        {
            board.EnableParticles(endColor);
        }
    }

    IEnumerator ShowGameOverUIDelayed(int winningTeamId)
    {
        yield return new WaitForSeconds(5f);

        ShowGameOverUIClientRpc(winningTeamId);
    }

    [ClientRpc]
    void ShowGameOverUIClientRpc(int winningTeamId)
    {
        if (gameOverImage == null || winText == null)
            return;

        gameOverImage.SetActive(true);

        if (winningTeamId == 0)
        {
            winText.text = "Team White Wins!";
        }
        else
        {
            winText.text = "Team Black Wins!";
        }
    }

    public bool HasEnoughGold(ulong clientId, int amount)
    {
        return GetGoldVar(clientId).Value >= amount;
    }

    public bool SpendGold(ulong clientId, int amount)
    {
        var goldVar = GetGoldVar(clientId);

        if (goldVar.Value < amount)
            return false;

        goldVar.Value -= amount;
        return true;
    }

    public int GetGold(ulong clientId)
    {
        return GetGoldVar(clientId).Value;
    }

}
