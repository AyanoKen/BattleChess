using Unity.Netcode;
using UnityEngine;

public class GamePhaseManager : NetworkBehaviour
{
    public static GamePhaseManager Instance;

    public enum GamePhase
    {
        Prep,
        Battle,
        Resolution
    }

    [SerializeField] private float prepDuration = 20f;

    public NetworkVariable<GamePhase> CurrentPhase =
        new NetworkVariable<GamePhase>(
            GamePhase.Prep,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public NetworkVariable<float> PhaseTimer =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartPrepPhase();
        }
    }

    void Update()
    {
        if (!IsServer) return;

        if (PhaseTimer.Value > 0f)
        {
            PhaseTimer.Value -= Time.deltaTime;

            if (PhaseTimer.Value <= 0f)
            {
                AdvancePhase();
            }
        }
    }

    void AdvancePhase()
    {
        switch (CurrentPhase.Value)
        {
            case GamePhase.Prep:
                StartBattlePhase();
                break;

            case GamePhase.Battle:
                StartResolutionPhase();
                break;
        }
    }

    void StartPrepPhase()
    {
        CurrentPhase.Value = GamePhase.Prep;
        PhaseTimer.Value = prepDuration;
        Debug.Log("Prep Phase");
    }

    void StartBattlePhase()
    {
        CurrentPhase.Value = GamePhase.Battle;
        PhaseTimer.Value = 0f;
        Debug.Log("Battle Phase");
    }

    void StartResolutionPhase()
    {
        CurrentPhase.Value = GamePhase.Resolution;
        PhaseTimer.Value = 0f;
        Debug.Log("Resolving Battle");
    }
}
