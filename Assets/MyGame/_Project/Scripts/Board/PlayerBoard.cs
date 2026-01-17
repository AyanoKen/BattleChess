using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// Script component on the player board prefab

public class PlayerBoard : NetworkBehaviour
{
    [Header("Bench")]
    public BoardSlot[] benchSlots;
    public BoardSlot[] boardSlots; 
    public BoardSlot[] enemySlots;

    [Header("End Game Particles")]
    [SerializeField] private ParticleSystem leftEmitter;
    [SerializeField] private ParticleSystem rightEmitter;

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

    // Function to capture board state during battle stage
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
                unitTypeId = slot.currentUnit.unitTypeId,
                sourceUnitId = slot.currentUnit.NetworkObjectId
            });
        }

        return state;
    }

    // ---------- Particle System ----------

    public void EnableParticles(Color color)
    {
        PlayEmitter(leftEmitter, color);
        PlayEmitter(rightEmitter, color);
    }

    void PlayEmitter(ParticleSystem ps, Color color)
    {
        if (ps == null)
            return;

        var main = ps.main;
        main.startColor = color;

        ps.gameObject.SetActive(true);
        ps.Play();
    }
}
