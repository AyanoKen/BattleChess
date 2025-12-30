using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class PlayerBoard : NetworkBehaviour
{
    [Header("Bench")]
    public BoardSlot[] benchSlots;
    public BoardSlot[] boardSlots; 
    public BoardSlot[] enemySlots;

    void Awake()
    {
        var allSlots = GetComponentsInChildren<BoardSlot>();

        benchSlots = allSlots
            .Where(s => s.slotType == BoardSlot.SlotType.Bench)
            .ToArray();

        boardSlots = allSlots
            .Where(s => s.slotType == BoardSlot.SlotType.Board)
            .ToArray();

        enemySlots = allSlots
            .Where(s => s.slotType == BoardSlot.SlotType.Enemy)
            .ToArray();
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

    public BoardSlot GetEnemySlotByIndex(int slotIndex)
    {
        foreach (var slot in enemySlots)
        {
            if (slot.slotIndex == slotIndex)
                return slot;
        }

        return null;
    }

    public List<UnitBoardState> CaptureBoardState()
    {
        List<UnitBoardState> state = new List<UnitBoardState>();

        foreach (var slot in boardSlots)
        {
            if (!slot.occupied || slot.currentUnit == null)
                continue;

            state.Add(new UnitBoardState
            {
                slotIndex = slot.slotIndex,
                unitTypeId = slot.currentUnit.unitTypeId
            });
        }

        return state;
    }

}
