using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlobShadowScript : MonoBehaviour
{
    public GameObject shadow;
    public Vector3 offset;
    
    private RaycastHit hit;
    
    private void FixedUpdate()
    {
        // Create a ray pointing straight down from the object's position
        Ray downRay = new Ray(
            new Vector3(
                transform.position.x,
                transform.position.y,
                transform.position.z
            ),
            -Vector3.up
        );
        
        // Cast the ray and check if it hits something
        if (Physics.Raycast(downRay, out hit))
        {
            // Optional debug output
            Debug.Log(hit.transform);
            
            // Get the hit position and apply offset
            Vector3 hitPosition = hit.point + offset;
            
            // Move the shadow to the hit position
            shadow.transform.position = hitPosition;
        }
    }
}