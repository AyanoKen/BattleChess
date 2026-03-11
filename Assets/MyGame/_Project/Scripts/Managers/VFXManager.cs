using UnityEngine;

// Logic for which VFX to use
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip bishopFireballSound;
    [SerializeField] private AudioClip queenAttackSound;
    [SerializeField] private AudioClip rookShockwaveSound;



    [Header("Bishop")]
    [SerializeField] private GameObject bishopFireballPrefab;
    [SerializeField] private GameObject bishopAOEImpactPrefab;

    [Header("Queen")]
    [SerializeField] private GameObject queenTargetRingPrefab;

    [Header("Rook")]
    [SerializeField] private GameObject rookShockwavePrefab;

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
                PlaySound(bishopFireballSound);
                break;

            case AttackVFXType.Bishop_AOE_Impact:
                SpawnAOEImpact(to);
                break;

            case AttackVFXType.Queen_TargetRing:
                SpawnQueenTargetRing(to);
                PlaySound(queenAttackSound);
                break;

            case AttackVFXType.Rook_Shockwave:
                SpawnRookShockwave(to);
                PlaySound(rookShockwaveSound);
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

    void SpawnRookShockwave(Vector3 position)
    {
        Instantiate(
            rookShockwavePrefab,
            position,
            Quaternion.identity
        );
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
            return;

        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(clip);
    }
}
