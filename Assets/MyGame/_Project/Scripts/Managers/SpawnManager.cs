using Unity.Netcode;
using UnityEngine;

// Script for spawning a pawn from shop
public class SpawnManager : NetworkBehaviour
{
    [SerializeField] private GameObject pawnPrefab;

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPawnServerRpc(ServerRpcParams rpcParams = default)
    {
        if (GamePhaseManager.Instance.CurrentPhase.Value != GamePhaseManager.GamePhase.Prep)
        {
            return;
        } 

        ulong clientId = rpcParams.Receive.SenderClientId;

        const int pawnCost = 1;

        if (!GamePhaseManager.Instance.HasEnoughGold(clientId, pawnCost))
        {
            Debug.Log($"Client {clientId} tried to buy pawn without gold");
            return;
        }

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

        GamePhaseManager.Instance.SpendGold(clientId, pawnCost);
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

        Vector3 correctedPos = slot.SnapPosition;
        correctedPos.y += unit.GetPlacementYOffset(slot);
        pawn.transform.position = correctedPos;

        pawn.GetComponent<NetworkObject>()
            .SpawnWithOwnership(board.OwnerClientId);

        unit.SnapToSlot(slot);
    }

}
