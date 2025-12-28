using UnityEngine;

public class SpawnButton : MonoBehaviour
{
    public void SpawnPawn()
    {
        if (SpawnManager.Instance == null)
        {
            Debug.LogError("SpawnManager not found");
            return;
        }

        SpawnManager.Instance.RequestSpawnPawnServerRpc();
    }
}
