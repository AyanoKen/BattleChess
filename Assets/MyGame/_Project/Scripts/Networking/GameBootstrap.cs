using Unity.Netcode;
using UnityEngine;

//Script for lobby
public class GameBootstrap : MonoBehaviour
{
    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    private void OnServerStarted()
    {
        Debug.Log("Server started — waiting for client...");
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        if (!NetworkManager.Singleton.IsServer)
            return;

        if (clientId == NetworkManager.ServerClientId)
            return;

        Debug.Log("All players connected. Loading game scene.");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Main",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }
}
