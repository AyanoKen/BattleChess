using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject pawnPrefab;

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        BoardManager boardManager = FindObjectOfType<BoardManager>();
        PlayerBoard board = boardManager.GetBoardForClient(clientId);

        if (board == null)
        {
            Debug.LogError($"No board found for client {clientId}");
            return;
        }

        BoardSlot slot = board.GetFreeBenchSlot();
        if (slot == null)
        {
            Debug.Log($"Bench full for client {clientId}");
            return;
        }

        SpawnPawn(board, slot);
    }

    void SpawnPawn(PlayerBoard board, BoardSlot slot)
    {
        GameObject pawn = Instantiate(
            pawnPrefab,
            slot.SnapPosition,
            Quaternion.identity
        );

        var unit = pawn.GetComponent<UnitController>();
        unit.teamId = board.OwnerClientId == NetworkManager.ServerClientId ? 0 : 1;

        var agent = pawn.GetComponent<UnityEngine.AI.NavMeshAgent>();
        agent.enabled = false;

        pawn.GetComponent<NetworkObject>()
            .SpawnWithOwnership(board.OwnerClientId);

        slot.Assign(unit);
    }

}
