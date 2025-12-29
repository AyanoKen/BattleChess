using UnityEngine;

public class BoardSlot : MonoBehaviour
{
    public bool occupied;
    public UnitController currentUnit;
    public int slotIndex;
    public Vector3 SnapPosition => transform.position;

    public enum SlotType
    {
        Bench, 
        Board
    }

    public SlotType slotType;

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
