using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

public class UnitDragHandler : NetworkBehaviour
{
    [SerializeField] private float hoverHeight = 2.2f;

    private Camera cam;
    private bool dragging;
    private Collider unitCollider;
    private NetworkTransform netTransform;

    void Start()
    {
        if (!IsOwner) return;

        cam = Camera.main;
        unitCollider = GetComponent<Collider>();
        netTransform = GetComponent<NetworkTransform>();
    }

    void OnMouseDown()
    {
        if (!IsOwner) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value != GamePhaseManager.GamePhase.Prep)
        {
            return;
        } 

        dragging = true;

        SetBoardTileVisibility(true);

        if (unitCollider != null)
        {
            unitCollider.enabled = false;
        }

        if (netTransform != null)
        {
            netTransform.enabled = false;
        }
    }

    void Update()
    {
        if (!dragging || !IsOwner) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value != GamePhaseManager.GamePhase.Prep)
        {
            return;
        } 

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Plane boardPlane = new Plane(Vector3.up, Vector3.zero);

        if (boardPlane.Raycast(ray, out float enter))
        {
            Vector3 p = ray.GetPoint(enter);
            transform.position = new Vector3(p.x, hoverHeight, p.z);
        }
    }

    void OnMouseUp()
    {
        if (!IsOwner) return;

        dragging = false;

        SetBoardTileVisibility(false);

        if (unitCollider != null)
        {
            unitCollider.enabled = true;
        }

        if (netTransform != null)
        {
            netTransform.enabled = true;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int slotIndex = -1;

        UnitController targetUnit = null;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            targetUnit = hit.collider.GetComponent<UnitController>();

            if (targetUnit == null)
            {
                BoardSlot slot = hit.collider.GetComponent<BoardSlot>();
                if (slot != null)
                {
                    slotIndex = slot.slotIndex;

                    if(slot.slotType == BoardSlot.SlotType.Enemy)
                    {
                        slotIndex = -1;   
                    }

                    if (slot.slotType == BoardSlot.SlotType.Bench)
                    {
                        if (GetComponent<UnitController>().unitType == UnitController.UnitType.King)
                        {
                            slotIndex = -1;
                        }
                    }
                    
                }

                SubmitDropServerRpc(slotIndex);
            }
            else //Dropped on another unit, so checking for fusion
            {
                SubmitFusionServerRpc(targetUnit.NetworkObjectId);
            }
            
        }
    }

    void SetBoardTileVisibility(bool visible)
    {
        foreach (var slot in Object.FindObjectsOfType<BoardSlot>())
        {
            if (slot.slotType != BoardSlot.SlotType.Board)
                continue;

            MeshRenderer mr = slot.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.enabled = visible;
            }
        }
    }

    [ServerRpc]
    void SubmitDropServerRpc(int slotIndex)
    {
        BoardManager bm = FindObjectOfType<BoardManager>();
        bm.TryPlaceUnit(GetComponent<UnitController>(), slotIndex);
    }

    [ServerRpc]
    void SubmitFusionServerRpc(ulong targetUnitId)
    {
        if (GamePhaseManager.Instance.CurrentPhase.Value
            != GamePhaseManager.GamePhase.Prep)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(targetUnitId, out var targetObj))
            return;

        var source = GetComponent<UnitController>();
        var target = targetObj.GetComponent<UnitController>();

        bool success = FusionManager.TryFuse(source, target);

        if (!success)
        {
            source.ReturnToSlot();
        }
    }

}
