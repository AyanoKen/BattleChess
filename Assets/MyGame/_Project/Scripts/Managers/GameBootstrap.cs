using Unity.Netcode;
using UnityEngine;

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
        Debug.Log("Server started");

        // Host loads Main scene
        NetworkManager.Singleton.SceneManager.LoadScene(
            "Main",
            UnityEngine.SceneManagement.LoadSceneMode.Single
        );
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }
}
