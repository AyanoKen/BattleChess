using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public bool occupied;
    public UnitController currentUnit;

    public Vector3 GetSnapPosition()
    {
        return transform.position;
    }
}
