using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class BoardManager : NetworkBehaviour
{
    [SerializeField] private GameObject boardPrefab;

    private readonly Dictionary<ulong, PlayerBoard> boardsByClient =
        new Dictionary<ulong, PlayerBoard>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            SpawnBoardForClient(clientId);
        }

        NetworkManager.OnClientConnectedCallback += SpawnBoardForClient;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.OnClientConnectedCallback -= SpawnBoardForClient;
        }

        base.OnDestroy();
    }

    void SpawnBoardForClient(ulong clientId)
    {
        if (boardsByClient.ContainsKey(clientId))
            return;

        GameObject boardGO = Instantiate(boardPrefab);

        boardGO.transform.position = GetBoardPosition(clientId);

        NetworkObject netObj = boardGO.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(clientId);

        PlayerBoard board = boardGO.GetComponent<PlayerBoard>();
        boardsByClient.Add(clientId, board);

        Debug.Log($"Spawned board for client {clientId}");
    }

    Vector3 GetBoardPosition(ulong clientId)
    {
        if (clientId == NetworkManager.ServerClientId)
            return Vector3.zero;

        return new Vector3(0, 0, 300); 
    }

    public PlayerBoard GetBoardForClient(ulong clientId)
    {
        boardsByClient.TryGetValue(clientId, out PlayerBoard board);
        return board;
    }

    public void ValidateAndPlaceUnit(UnitController unit, Vector3 dropPosition)
    {
        if (!IsServer) return;

        PlayerBoard board = GetBoardForClient(unit.OwnerClientId);
        if (board == null) return;

        if (board.IsInsideBoard(dropPosition) || board.IsInsideBench(dropPosition))
        {
            Vector3 correctedPos = dropPosition;
            correctedPos.y += unit.GetPlacementYOffset();
            unit.transform.position = correctedPos;
            return;
        }

        RevertUnit(unit);
    }

    void RevertUnit(UnitController unit)
    {
        if (unit.CurrentSlot == null)
            return;

        Vector3 pos = unit.CurrentSlot.SnapPosition;
        pos.y += unit.GetPlacementYOffset();
        unit.transform.position = pos;
    }

}
