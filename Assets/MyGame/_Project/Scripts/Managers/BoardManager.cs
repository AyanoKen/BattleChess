using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode.Components;

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

    public void TryPlaceUnit(UnitController unit, int slotIndex)
    {
        if (!IsServer) return;

        PlayerBoard board = GetBoardForClient(unit.OwnerClientId);
        if (board == null || slotIndex < 0)
        {
            RevertUnit(unit);
            return;
        }

        BoardSlot slot = board.GetSlotByIndex(slotIndex);
        if (slot == null || slot.occupied)
        {
            RevertUnit(unit);
            return;
        }

        unit.SnapToSlot(slot);
    }


    void RevertUnit(UnitController unit)
    {
        if (unit.CurrentSlot == null) return;

        BoardSlot slot = unit.CurrentSlot;

        Collider unitCol = unit.GetComponent<Collider>();
        Collider slotCol = slot.GetComponent<Collider>();

        float yOffset = unitCol.bounds.extents.y + slotCol.bounds.extents.y;

        Vector3 pos = slot.SnapPosition;
        pos.y += yOffset;

        unit.transform.position = pos;
    }



    [ClientRpc]
    void EnableNetworkTransformClientRpc(
        ulong unitNetworkId,
        ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(unitNetworkId, out var netObj))
            return;

        var netTransform = netObj.GetComponent<NetworkTransform>();
        if (netTransform != null)
            netTransform.enabled = true;
    }

    [ClientRpc]
    void RevertUnitClientRpc(
        ulong unitNetworkId,
        Vector3 revertPosition,
        ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(unitNetworkId, out var netObj))
            return;

        netObj.transform.position = revertPosition;
    }

    [ClientRpc]
    void PlaceUnitClientRpc(
        ulong unitNetworkId,
        Vector3 finalPosition,
        ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(unitNetworkId, out var netObj))
            return;

        netObj.transform.position = finalPosition;
    }

}
