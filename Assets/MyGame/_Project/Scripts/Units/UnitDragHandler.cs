using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Components;

public class UnitDragHandler : NetworkBehaviour
{
    
    private Camera cam;
    private bool dragging;
    private Vector3 dragOffset;
    private NetworkTransform netTransform;

    void Start()
    {
        if (!IsOwner) return;

        cam = Camera.main;
        netTransform = GetComponent<NetworkTransform>();
    }

    void OnMouseDown()
    {
        if (!IsOwner) return;

        dragging = true;

        if (netTransform != null)
        {
            netTransform.enabled = false;
        }

        Plane plane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float enter))
        {
            dragOffset = transform.position - ray.GetPoint(enter);
        }
    }

    void OnMouseUp()
    {
        if (!IsOwner) return;

        dragging = false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            BoardSlot slot = hit.collider.GetComponent<BoardSlot>();
            if (slot == null)
            {
                SubmitDropServerRpc(-1);
                return;
            }

            SubmitDropServerRpc(slot.slotIndex);
        }
        
    }

    void Update()
    {
        if (!dragging || !IsOwner) return;

        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float enter))
        {
            transform.position = ray.GetPoint(enter) + dragOffset;
        }
    }

    [ServerRpc]
    void SubmitDropServerRpc(int slotIndex)
    {
        UnitController unit = GetComponent<UnitController>();
        BoardManager boardManager = FindObjectOfType<BoardManager>();

        boardManager.TryPlaceUnit(unit, slotIndex);
    }
}
