using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testGroundCheck : MonoBehaviour
{

    public LayerMask whatIsGround;
    // Update is called once per frame
    void Update()
    {
        RaycastHit hitInfo;
        Color rayColor;
        Collider col = GetComponent<Collider>();
        if (Physics.Raycast(col.bounds.center, Vector3.down, out hitInfo, col.bounds.extents.y + 5f, whatIsGround))
        {
            rayColor = Color.green;
        }
        else 
        {
            rayColor= Color.red;
        }
        Debug.DrawRay(col.bounds.center, Vector3.down * (col.bounds.extents.y + 5f), rayColor);
    }
}
