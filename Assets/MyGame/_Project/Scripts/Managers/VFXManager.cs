using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Bishop")]
    [SerializeField] private GameObject bishopFireballPrefab;
    [SerializeField] private GameObject bishopAOEImpactPrefab;

    [Header("Queen")]
    [SerializeField] private GameObject queenTargetRingPrefab;

    void Awake()
    {
        Instance = this;
    }

    public void Play(
        AttackVFXType type,
        Vector3 from,
        Vector3 to
    )
    {
        switch (type)
        {
            case AttackVFXType.Bishop_Fireball:
                SpawnFireball(from, to);
                break;

            case AttackVFXType.Bishop_AOE_Impact:
                SpawnAOEImpact(to);
                break;

            case AttackVFXType.Queen_TargetRing:
                SpawnQueenTargetRing(to);
                break;
        }
    }

    void SpawnFireball(Vector3 from, Vector3 to)
    {
        GameObject fx = Instantiate(
            bishopFireballPrefab,
            from,
            Quaternion.identity
        );

        fx.GetComponent<FireballVFX>()
          .Init(from, to);
    }

    void SpawnAOEImpact(Vector3 position)
    {
        Instantiate(
            bishopAOEImpactPrefab,
            position,
            Quaternion.identity
        );
    }

    void SpawnQueenTargetRing(Vector3 position)
    {
        GameObject ring = Instantiate(
            queenTargetRingPrefab,
            Vector3.zero,
            Quaternion.identity
        );

        ring.GetComponent<VFXRingDrop>()
            .Init(position);
    }
}
