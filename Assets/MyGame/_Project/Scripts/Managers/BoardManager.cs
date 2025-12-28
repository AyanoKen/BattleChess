using Unity.Netcode;
using UnityEngine;

public class BoardManager : NetworkBehaviour
{
    public static BoardManager Instance;

    [SerializeField] private GameObject boardPrefab;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
        {
            SpawnBoardForClient(clientId);
        }
    }

    void SpawnBoardForClient(ulong clientId)
    {
        GameObject board = Instantiate(boardPrefab);

        int teamId = clientId == NetworkManager.ServerClientId ? 0 : 1;

        board.transform.position = teamId == 0
            ? new Vector3(0, 0, 0)
            : new Vector3(0, 0, 100);

        board.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
    }
}
