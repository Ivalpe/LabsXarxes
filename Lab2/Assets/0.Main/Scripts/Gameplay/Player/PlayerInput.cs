using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    ClientNetworkManager networkManager;

    //timer
    private float interval = 1f / 30f;
    private float timer = 0f;

    private void Start()
    {
        //get network manager
        networkManager = ClientNetworkManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        //only send player info on timer to avoid sending too many packages
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            SendPlayerInput(); 
        }
    }

    void SendPlayerInput()
    {
        //get input axis
        float horizontal = Input.GetAxis("Horizontal"); // A (-1) | D (+1)
        float vertical = Input.GetAxis("Vertical"); // S (-1) | W (+1)

        //get camera yaw (to know what "forward" is)
        float yawRotation;
        if (cameraTransform != null) yawRotation = cameraTransform.eulerAngles.y;
        else yawRotation = transform.eulerAngles.y; //if no camera -> get transform to avoid errors

        //send by UDP
        byte[] inputPacket = networkManager.serializer.SerializePlayerInput(networkManager.userId, horizontal, vertical, yawRotation);
        networkManager.UDPconnection.SendPacket(inputPacket);
    }
}
