using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using System.Threading.Tasks;

public class RelayHostManager : MonoBehaviour
{
    public async Task<string> StartRelayHost(int maxPlayers)
    {
        Allocation allocation =
            await RelayService.Instance.CreateAllocationAsync(maxPlayers);

        string joinCode =
            await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData
        );

        NetworkManager.Singleton.StartHost();

        Debug.Log($"Relay Host started. Join Code: {joinCode}");

        return joinCode;
    }
}
