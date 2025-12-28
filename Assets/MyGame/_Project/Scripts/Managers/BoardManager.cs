using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class BoardManager : NetworkBehaviour
{
    [SerializeField] private GameObject boardPrefab;

    private HashSet<ulong> spawnedBoards = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Spawn for host
        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            TrySpawnBoard(clientId);
        }

        // Listen for late-joining clients
        NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
        }

        base.OnDestroy();
    }

    void OnClientConnected(ulong clientId)
    {
        TrySpawnBoard(clientId);
    }

    void TrySpawnBoard(ulong clientId)
    {
        if (spawnedBoards.Contains(clientId))
            return;

        GameObject board = Instantiate(boardPrefab);

        board.GetComponent<NetworkObject>()
             .SpawnWithOwnership(clientId);

        spawnedBoards.Add(clientId);

        Debug.Log($"Spawned board for client {clientId}");
    }
}
