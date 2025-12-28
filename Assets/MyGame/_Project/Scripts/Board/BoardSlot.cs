using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public bool occupied;
    public UnitController currentUnit;

    public Vector3 SnapPosition => transform.position;

    public void Assign(UnitController unit)
    {
        occupied = true;
        currentUnit = unit;
    }

    public void Clear()
    {
        occupied = false;
        currentUnit = null;
    }
}
