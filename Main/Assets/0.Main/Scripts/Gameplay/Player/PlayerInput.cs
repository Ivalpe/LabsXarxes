using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    //timer
    private float interval = 1f / 30f;
    private float timer = 0f;
    bool rotatedTile = false;

    //public Collider collider;
    public Rigidbody rb;

    private void Start()
    {
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

        // if client
        if (ClientNetworkManager.Instance != null)
        {
            byte[] packet = ClientNetworkManager.Instance.serializer.SerializePlayerInput(
                ClientNetworkManager.Instance.userId, horizontal, vertical, yawRotation, sendJump
            );
            //via udp
            ClientNetworkManager.Instance.UDPconnection.SendPacket(packet);
        }
        else if (ConnectionManager.Instance != null) // if server
        {
            var server = ConnectionManager.Instance;
            if (server.playerInfoDic.TryGetValue(server.hostPlayerId, out PlayerInfo hostPlayer))
            {
                hostPlayer.SetInput(horizontal, vertical, yawRotation, sendJump);
            }
        }

        sendJump = false;

    }
}
