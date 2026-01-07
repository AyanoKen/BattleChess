using UnityEngine;

public class QueenRingPulse : MonoBehaviour
{
    [SerializeField] private float pulseDuration = 0.25f;
    [SerializeField] private float glowMultiplier = 2.5f;

    private Material mat;
    private Color baseColor;
    private float timer;
    private bool pulsing;

    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            mat = r.material;
            baseColor = mat.color;
        }
    }

    public void Pulse()
    {
        timer = 0f;
        pulsing = true;
    }

    void Update()
    {
        if (!pulsing || mat == null)
            return;

        timer += Time.deltaTime;
        float t = timer / pulseDuration;

        if (t >= 1f)
        {
            mat.color = baseColor;
            pulsing = false;
            return;
        }

        float intensity = Mathf.Lerp(
            glowMultiplier,
            1f,
            t
        );

        mat.color = baseColor * intensity;
    }
}
