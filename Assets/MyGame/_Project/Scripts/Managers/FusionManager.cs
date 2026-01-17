using Unity.Netcode;
using UnityEngine;

// Script for fusion logic
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

        if (source.unitType == UnitController.UnitType.Queen) // Queen doesnt fuse
            return false;

        if (source.unitType == UnitController.UnitType.King) // King doesnt fuse
            return false;


        FuseUnit(source, target);
        return true;
    }

    static void FuseUnit(UnitController source, UnitController target)
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
        else
        {
            survivor.UpdateFusionCountClientRpc(survivor.fusionCount);
        }

    }

    static void PromotePawn(UnitController unit)
    {
        BoardSlot slot = unit.CurrentSlot;
        ulong ownerId = unit.OwnerClientId;
        float carriedHp = unit.GetHP();
        int teamId = unit.teamId;

        if (slot == null)
            return;

        UnitController.UnitType defaultType;

        if (unit.unitType == UnitController.UnitType.Pawn)
        {
            defaultType = UnitController.UnitType.Rook;
        }
        else
        {
            defaultType = UnitController.UnitType.Queen;
        }

        int typeId = (int)defaultType;
        GameObject prefab =
            GamePhaseManager.Instance.GetBattlePrefab(typeId);

        if (prefab == null)
        {
            Debug.Log("Fusion Failed here");
            return;
        }

        Vector3 spawnPos = unit.transform.position;
        Quaternion spawnRot = unit.transform.rotation;

        unit.GetComponent<Unity.Netcode.NetworkObject>().Despawn(true);

        GameObject upgraded = Object.Instantiate(
            prefab,
            spawnPos,
            spawnRot
        );

        var controller = upgraded.GetComponent<UnitController>();
        controller.unitType = defaultType;
        controller.fusionCount = 0;
        controller.SetHP(carriedHp + controller.maxHP);
        controller.teamId = teamId;

        upgraded.GetComponent<Unity.Netcode.NetworkObject>()
            .SpawnWithOwnership(ownerId);

        controller.SnapToSlot(slot);

        if (unit.unitType == UnitController.UnitType.Pawn)
        {
            GamePhaseManager.Instance.ShowPromotionUIClientRpc(
                controller.NetworkObjectId,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { ownerId }
                    }
                }
            );
        }
    }

}
