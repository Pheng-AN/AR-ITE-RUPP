using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (cam != null)
        {
            Quaternion targetRotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
        }
    }
}

