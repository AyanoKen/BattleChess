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

        survivor.AddHP(consumed.GetHP());
        survivor.fusionCount++;

        consumed.GetComponent<NetworkObject>().Despawn(true);

        if (survivor.fusionCount >= 2)
        {
            PromotePawn(survivor);
        }

    }

    static void PromotePawn(UnitController pawn)
    {
        BoardSlot slot = pawn.CurrentSlot;
        ulong ownerId = pawn.OwnerClientId;

        if (slot == null)
            return;

        float carriedHp = pawn.GetHP();

        UnitController.UnitType newType = UnitController.UnitType.Rook;
        int newTypeId = (int)newType;

        GameObject prefab =
            GamePhaseManager.Instance.GetBattlePrefab(newTypeId);

        if (prefab == null)
        {
            Debug.Log("Fusion Failed here");
            return;
        }

        Vector3 spawnPos = pawn.transform.position;
        Quaternion spawnRot = pawn.transform.rotation;

        pawn.GetComponent<Unity.Netcode.NetworkObject>().Despawn(true);

        GameObject upgraded = Object.Instantiate(
            prefab,
            spawnPos,
            spawnRot
        );

        var controller = upgraded.GetComponent<UnitController>();
        controller.unitType = newType;
        controller.fusionCount = 0;
        controller.SetHP(carriedHp + controller.maxHP);

        upgraded.GetComponent<Unity.Netcode.NetworkObject>()
            .SpawnWithOwnership(ownerId);

        controller.SnapToSlot(slot);

        Debug.Log("Pawn promoted to Rook");
    }

}
