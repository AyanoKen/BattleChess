using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerBoard : NetworkBehaviour
{
    [Header("Bench")]
    public BoardSlot[] benchSlots;

    public Collider boardBounds;
    public Collider benchBounds;

    void Awake()
    {
        if (boardBounds == null)
        {
            boardBounds = transform.Find("BoardBounds").GetComponent<Collider>();
        }

        if (benchBounds != null)
        {
            benchBounds = transform.Find("BenchBounds")?.GetComponent<Collider>();   
        }

        benchSlots = GetComponentsInChildren<BoardSlot>()
            .Where(slot => slot.gameObject.name.Contains("Bench"))
            .ToArray();
    }

    public BoardSlot GetFreeBenchSlot()
    {
        return benchSlots.FirstOrDefault(slot => !slot.occupied);
    }

    public bool IsInsideBoard(Vector3 pos)
    {
        return boardBounds.bounds.Contains(pos);
    }

    public bool IsInsideBench(Vector3 pos)
    {
        return benchBounds != null && benchBounds.bounds.Contains(pos);
    }
}
