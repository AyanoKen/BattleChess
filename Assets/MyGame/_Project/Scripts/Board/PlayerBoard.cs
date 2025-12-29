using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class PlayerBoard : NetworkBehaviour
{
    [Header("Bench")]
    public BoardSlot[] benchSlots;
    
    public BoardSlot[] boardSlots; 

    void Awake()
    {
        benchSlots = GetComponentsInChildren<BoardSlot>()
            .Where(slot => slot.slotType == BoardSlot.SlotType.Bench)
            .ToArray();

        for (int i = 0; i < benchSlots.Length; i++)
        {
            benchSlots[i].slotIndex = i;
        }

        boardSlots = GetComponentsInChildren<BoardSlot>()
            .Where(slot => slot.slotType == BoardSlot.SlotType.Board)
            .ToArray();

        int boardOffset = benchSlots.Length;

        for (int i = 0; i < boardSlots.Length; i++)
        {
            boardSlots[i].slotIndex = boardOffset + i;
        }
    }

    public BoardSlot GetFreeBenchSlot()
    {
        return benchSlots.FirstOrDefault(slot => !slot.occupied);
    }

    public BoardSlot GetSlotByIndex(int index)
    {
        foreach (var slot in benchSlots)
        {
            if (slot.slotIndex == index)
                return slot;
        }

        foreach (var slot in boardSlots)
        {
            if (slot.slotIndex == index)
                return slot;
        }

        return null;
    }
}
