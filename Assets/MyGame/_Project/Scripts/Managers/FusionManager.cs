using Unity.Netcode;
using UnityEngine;

public static class FusionManager
{
    public static bool TryFuse(UnitController source, UnitController target)
    {
        if (source == null || target == null)
            return false;

        if (source.OwnerClientId != target.OwnerClientId)
            return false;

        if (source.unitType != target.unitType)
            return false;

        // Only pawn fusion for now, change this later
        if (source.unitType != UnitController.UnitType.Pawn) 
            return false;

        FusePawn(source, target);
        return true;
    }

    static void FusePawn(UnitController source, UnitController target)
    {
        UnitController survivor = target;
        UnitController consumed = source;

        survivor.fusionCount++;

        survivor.unitType = UnitController.UnitType.Rook;

        // Swap prefab later – for now just log
        Debug.Log("Pawn fused into Rook!");

        consumed.GetComponent<NetworkObject>().Despawn(true);
    }
}
