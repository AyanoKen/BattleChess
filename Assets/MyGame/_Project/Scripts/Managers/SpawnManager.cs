using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private GameObject pawnPrefab;

    void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        PlayerBoard board = FindBoardForClient(clientId);
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

    PlayerBoard FindBoardForClient(ulong clientId)
    {
        return FindObjectsOfType<PlayerBoard>()
            .FirstOrDefault(board => board.OwnerClientId == clientId);
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

        NetworkObject netObj = pawn.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(board.OwnerClientId);

        slot.Assign(unit);
    }
}
