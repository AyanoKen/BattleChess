using UnityEngine;

public class VFXRingExpand : MonoBehaviour
{
    [Header("Scale")]
    [SerializeField] private float startRadius = 0.2f;
    [SerializeField] private float endRadius = 2.5f;
    [SerializeField] private float duration = 0.35f;

    [Header("Fade")]
    [SerializeField] private bool fadeOut = true;

    private Material mat;
    private float timer;
    private float fixedYScale;

    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            mat = r.material; 
        }

        fixedYScale = transform.localScale.y;

        transform.localScale = new Vector3(
            startRadius,
            fixedYScale,
            startRadius
        );
    }

    void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        float radius = Mathf.Lerp(startRadius, endRadius, t);

        transform.localScale = new Vector3(
            radius,
            fixedYScale,
            radius
        );

        if (fadeOut && mat != null && mat.HasProperty("_Color"))
        {
            Color c = mat.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            mat.color = c;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
