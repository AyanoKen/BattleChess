using UnityEngine;

public class FireballVFX : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    private Vector3 target;

    public void Init(Vector3 from, Vector3 to)
    {
        transform.position = from;
        target = to;
    }

    void Update()
    {
        transform.position =
            Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            Destroy(gameObject);
        }
    }
}
