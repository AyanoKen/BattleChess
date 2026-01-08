using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public void SpawnPawn()
    {
        SpawnManager spawnManager = FindObjectOfType<SpawnManager>();

        if (spawnManager == null)
        {
            Debug.LogError("SpawnManager not found in scene");
            return;
        }

        spawnManager.RequestSpawnPawnServerRpc();
    }
}

