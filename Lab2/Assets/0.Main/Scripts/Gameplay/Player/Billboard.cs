using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform camTransform;

    private void Start()
    {
        // searches local manera (main)
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        // if camera destroys, search reference
        if (camTransform == null && Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }

        if (camTransform != null)
        {
            // canvas copies rotation from camera
            transform.rotation = camTransform.rotation;
        }
    }
}