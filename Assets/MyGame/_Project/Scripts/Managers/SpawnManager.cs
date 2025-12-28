using Unity.Netcode;
using UnityEngine;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private GameObject pawnPrefab;
    [SerializeField] private Transform spawnArea;


    void Awake()
    {
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestSpawnPawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int teamId = GetTeamFromClientId(clientId);

        SpawnPawn(teamId);
    }

    void SpawnPawn(int teamId)
    {
        GameObject pawn = Instantiate(
            pawnPrefab,
            GetRandomSpawnPoint(),
            Quaternion.identity
        );

        var unit = pawn.GetComponent<UnitController>();
        unit.teamId = teamId;

        pawn.GetComponent<NetworkObject>().Spawn();
    }

    int GetTeamFromClientId(ulong clientId)
    {
        // Host is clientId 0, client is 1
        return clientId == NetworkManager.ServerClientId ? 0 : 1;
    }

    Vector3 GetRandomSpawnPoint()
    {
        Vector3 center = spawnArea.position;
        Vector3 size = spawnArea.localScale / 2f;

        return center + new Vector3(
            Random.Range(-size.x, size.x),
            0f,
            Random.Range(-size.z, size.z)
        );
    }
}
