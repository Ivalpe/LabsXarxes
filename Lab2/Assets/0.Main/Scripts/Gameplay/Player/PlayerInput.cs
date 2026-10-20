using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    ClientNetworkManager networkManager;

    //timer
    private float interval = 1f / 30f;
    private float timer = 0f;
    bool sendJump = false, sendGrounded = false;

    //public Collider collider;
    public Rigidbody rb;

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
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            SendPlayerInput();
            timer = 0f;
        }
        //Vector3 pos = transform.position;
        //Debug.DrawRay(pos, Vector3.down*0.5f, Color.red);
        
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

        if (Input.GetButtonDown("Jump"))
        {
            sendJump = true;
        }


        if (IsGrounded())
        {
            sendGrounded = true;
        }

        //if (jump)
        //{
        //    Debug.Log("Pressed Space");
        //}

        //bool clicked = Input.GetMouseButtonDown(0);

        //send by UDP
        byte[] inputPacket = networkManager.serializer.SerializePlayerInput(networkManager.userId, horizontal, vertical, yawRotation, sendJump, sendGrounded);
        networkManager.UDPconnection.SendPacket(inputPacket);
        sendJump = false;
        sendGrounded = IsGrounded();
    }

    bool IsGrounded()
    {

        Vector3 pos = new Vector3(transform.position.x, transform.position.y, transform.position.z);

        //Raising origin slightly so the ray starts INSIDE the body and avoids ground intersection
        float originOffset = 0.2f;
        Vector3 raycastOrigin = pos + Vector3.up * originOffset;

        //raycast dist = offset + ground check range
        float distanceThreshold = 0.5f;
        float raycastDistance = distanceThreshold + originOffset;
        Debug.DrawRay(raycastOrigin, Vector3.down, Color.red);

        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, raycastDistance))
        {
            // Ignore hitting the player's own collider if raycast originates inside it
            if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }
}
