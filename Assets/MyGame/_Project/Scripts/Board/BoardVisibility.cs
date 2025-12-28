using Unity.Netcode;
using UnityEngine;

public class BoardVisibility : NetworkBehaviour
{
    void Start()
    {
        if (!IsOwner)
        {
            gameObject.SetActive(false);
        }
    }
}
