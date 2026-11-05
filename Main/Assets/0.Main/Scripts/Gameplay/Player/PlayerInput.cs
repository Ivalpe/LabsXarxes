using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    ClientNetworkManager networkManager;

    //timer
    private float interval = 1f / 30f;
    private float timer = 0f;
    bool rotatedTile = false;

    //public Collider collider;
    public Rigidbody rb;

    private void OnEnable()
    {

    }

    private void Start()
    {
        //get network manager
        networkManager = ClientNetworkManager.Instance;
        //collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //only send player info on timer to avoid sending too many packages

        if (Input.GetButtonDown("Jump"))
        {
           
            SendPlayerInput(true);
        }

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            SendPlayerInput(false);
            timer = 0f;
        }
        //Vector3 pos = transform.position;
        //Debug.DrawRay(pos, Vector3.down*0.5f, Color.red);
        

    }



    void SendPlayerInput(bool sendJump)
    {
        //get input axis
        float horizontal = Input.GetAxis("Horizontal"); // A (-1) | D (+1)
        float vertical = Input.GetAxis("Vertical"); // S (-1) | W (+1)

        //get camera yaw (to know what "forward" is)
        float yawRotation;
        if (cameraTransform != null) yawRotation = cameraTransform.eulerAngles.y;
        else yawRotation = transform.eulerAngles.y; //if no camera -> get transform to avoid errors



        //send by UDP
        byte[] inputPacket = networkManager.serializer.SerializePlayerInput(networkManager.userId, horizontal, vertical, yawRotation, sendJump);
        networkManager.UDPconnection.SendPacket(inputPacket);
        sendJump = false;
        
    }


}
