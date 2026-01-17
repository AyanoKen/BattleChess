using System;

// Public structure for capturing player board state. Used during battle stage
[Serializable]
public struct UnitBoardState
{
    public int slotIndex;
    public int unitTypeId;
    public ulong sourceUnitId;
}
