using UnityEngine;

public class CubeRotation : MonoBehaviour
{

    private void Start()
    {
        //find the client network manager game object
       ClientNetworkManager.Instance.RegisterCube(this.transform);
    }
}
