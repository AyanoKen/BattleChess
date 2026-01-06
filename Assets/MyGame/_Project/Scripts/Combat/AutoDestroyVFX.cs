using UnityEngine;

public class AutoDestroyVFX : MonoBehaviour
{
    [SerializeField] float lifetime = 1.5f;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
