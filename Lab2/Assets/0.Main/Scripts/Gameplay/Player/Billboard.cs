using UnityEngine;

public class Billboard : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    Camera cam;
    
    void LateUpdate()
    {
        if (cam == null || !cam.isActiveAndEnabled)
        {
            cam = Camera.main;
            if (cam == null) return;
        }

        transform.rotation = cam.transform.rotation;
    }

   
}
