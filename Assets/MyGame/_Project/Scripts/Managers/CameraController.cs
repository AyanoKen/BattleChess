using Unity.Netcode;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    void Start()
    {
        if (!NetworkManager.Singleton.IsClient) return;

        Invoke(nameof(PositionCamera), 0.2f);

        GamePhaseManager.Instance.CurrentPhase.OnValueChanged += OnPhaseChanged;
    }

    void PositionCamera()
    {
        foreach (var board in FindObjectsOfType<PlayerBoard>())
        {
            if (board.OwnerClientId == NetworkManager.Singleton.LocalClientId)
            {
                MoveCameraToBoard(board);
                return;
            }
        }

        Debug.LogWarning("Local board not found yet");
    }

    void MoveCameraToBoard(PlayerBoard board)
    {
        Camera.main.transform.position =
            board.transform.position + new Vector3(10, 10, 0);

        Camera.main.transform.LookAt(board.transform.position);
    }

    void OnPhaseChanged(
    GamePhaseManager.GamePhase oldPhase,
    GamePhaseManager.GamePhase newPhase)
    {
        if (newPhase != GamePhaseManager.GamePhase.Battle)
            return;

        MoveCameraToHostBoard();
    }

    void MoveCameraToHostBoard()
    {
        foreach (var board in FindObjectsOfType<PlayerBoard>())
        {
            if (board.OwnerClientId == NetworkManager.ServerClientId)
            {
                Camera.main.transform.position =
                    board.transform.position + new Vector3(10, 10, 0);

                Camera.main.transform.LookAt(board.transform.position);
                return;
            }
        }
    }


}
