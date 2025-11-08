using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
    }

    void Update()
    {
        if (playerCamera != null)
        {
            transform.LookAt(playerCamera.transform);
            transform.Rotate(0, 180, 0); // ·­×ª180¶È
        }
    }
}
