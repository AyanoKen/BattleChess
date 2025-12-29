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
            var unit = GetComponent<UnitController>();

            Vector3 correctedPos = hit.point;
            correctedPos.y += unit.GetPlacementYOffset();
            transform.position = correctedPos;

            SubmitDropServerRpc(hit.point);
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
    void SubmitDropServerRpc(Vector3 dropPosition)
    {
        BoardManager boardManager = FindObjectOfType<BoardManager>();
        boardManager.ValidateAndPlaceUnit(this.GetComponent<UnitController>(), dropPosition);
    }
}
