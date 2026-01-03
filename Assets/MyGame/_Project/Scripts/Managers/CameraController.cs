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
            board.transform.position + new Vector3(8, 8, 0);

        Camera.main.transform.LookAt(board.transform.position);

        Vector3 euler = Camera.main.transform.eulerAngles;
        euler.x = 55f;
        Camera.main.transform.eulerAngles = euler;
    }

    void OnPhaseChanged(
    GamePhaseManager.GamePhase oldPhase,
    GamePhaseManager.GamePhase newPhase)
    {
        if (newPhase == GamePhaseManager.GamePhase.Battle)
        {
            MoveCameraToBattleView();
        }
        else if (newPhase == GamePhaseManager.GamePhase.Prep)
        {
            PositionCamera();
        }
    }

    void MoveCameraToBattleView()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var board in FindObjectsOfType<PlayerBoard>())
        {
            if (board.OwnerClientId == NetworkManager.ServerClientId)
            {
                Vector3 baseOffset = new Vector3(8, 8, 0);

                Camera.main.transform.position =
                    board.transform.position + baseOffset;

                Camera.main.transform.LookAt(board.transform.position);

                Vector3 euler = Camera.main.transform.eulerAngles;
                euler.x = 55f;
                Camera.main.transform.eulerAngles = euler;

                if (localId != NetworkManager.ServerClientId)
                {
                    Camera.main.transform.RotateAround(
                        board.transform.position,
                        Vector3.up,
                        180f
                    );
                }

                return;
            }
        }
    }

}
