using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerBoard : NetworkBehaviour
{
    [Header("Bench")]
    public BoardSlot[] benchSlots;

    void Awake()
    {
        benchSlots = GetComponentsInChildren<BoardSlot>()
            .Where(slot => slot.gameObject.name.Contains("Bench"))
            .ToArray();
    }

    public BoardSlot GetFreeBenchSlot()
    {
        return benchSlots.FirstOrDefault(slot => !slot.occupied);
    }
}
