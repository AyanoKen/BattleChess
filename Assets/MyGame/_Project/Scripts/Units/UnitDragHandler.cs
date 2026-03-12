using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

public class UnitDragHandler : NetworkBehaviour
{
    [SerializeField] private float hoverHeight = 2.2f;

    [SerializeField] private GameObject rangeIndicatorPrefab;

    private GameObject activeRangeIndicator;
    private float rangeIndicatorTimer;

    private Camera cam;
    private bool dragging;
    private Collider unitCollider;
    private NetworkTransform netTransform;

    void Start()
    {
        if (!IsOwner) return;

        cam = null;
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
        if (IsOwner && Input.GetMouseButtonDown(1))
        {
            TryShowRange();
        }

        if (activeRangeIndicator != null)
        {
            rangeIndicatorTimer -= Time.deltaTime;

            if (rangeIndicatorTimer <= 0f)
            {
                Destroy(activeRangeIndicator);
                activeRangeIndicator = null;
            }
        }

        if (!dragging || !IsOwner) return;

        if (GamePhaseManager.Instance.CurrentPhase.Value != GamePhaseManager.GamePhase.Prep)
        {
            return;
        } 

        Camera camera = GetCamera();
        if (camera == null)
            return;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
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

        Camera camera = GetCamera();
        if (camera == null)
            return;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
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

    void TryShowRange()
    {
        Camera camera = GetCamera();
        if (camera == null)
            return;

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider.gameObject != gameObject)
            return;

        UnitController unit = GetComponent<UnitController>();
        if (unit == null)
            return;

        ShowRange(unit.attackRange);
    }

    void ShowRange(float range)
    {
        if (rangeIndicatorPrefab == null)
            return;

        if (activeRangeIndicator != null)
            Destroy(activeRangeIndicator);

        Vector3 pos = transform.position;
        pos.y -= 0.5f;

        activeRangeIndicator = Instantiate(
            rangeIndicatorPrefab,
            pos,
            Quaternion.identity
        );

        activeRangeIndicator.transform.localScale =
            new Vector3(range * 2f, 0.01f, range * 2f);

        rangeIndicatorTimer = 1f;
    }

    Camera GetCamera()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        return cam;
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
