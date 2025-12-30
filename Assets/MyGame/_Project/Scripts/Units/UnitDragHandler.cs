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

        if (unitCollider != null)
            unitCollider.enabled = true;

        if (netTransform != null)
            netTransform.enabled = true;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        int slotIndex = -1;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            BoardSlot slot = hit.collider.GetComponent<BoardSlot>();
            if (slot != null)
                slotIndex = slot.slotIndex;
        }

        SubmitDropServerRpc(slotIndex);
    }

    [ServerRpc]
    void SubmitDropServerRpc(int slotIndex)
    {
        BoardManager bm = FindObjectOfType<BoardManager>();
        bm.TryPlaceUnit(GetComponent<UnitController>(), slotIndex);
    }
}
