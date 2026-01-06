using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Bishop")]
    [SerializeField] private GameObject bishopFireballPrefab;
    [SerializeField] private GameObject bishopAOEImpactPrefab;

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
}
