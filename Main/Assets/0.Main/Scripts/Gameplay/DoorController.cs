using UnityEngine;
using TMPro;

public class DoorController : MonoBehaviour
{
    [Header("Door Mesh Transforms")]
    public Transform door_left;
    public Transform door_right;

    //Limit for the opening of the door
    public Transform OpenedLeftPos;
    public Transform OpenedRightPos;

    private Vector3 ClosedLeftPos;
    private Vector3 ClosedRightPos;

    //ConnectionManager connectionManager;

    public TextMeshPro timer_text;
    private ConnectionManager serverManager;

    void Start()
    {
        if (serverManager == null) serverManager = FindAnyObjectByType<ConnectionManager>();

        if (door_left != null && door_right != null)
        {
            ClosedLeftPos = door_left.position;
            ClosedRightPos = door_right.position;
        }

    }
    //Slowly closes the door until its fully closed from the previously setted up pos
    void Update()
    {
        float networkOpening = 0.0f;

        if (ClientNetworkManager.Instance != null)
        {
            networkOpening = ClientNetworkManager.Instance.currentDoorOpening;
        }
        else
        {
            if(serverManager==null) serverManager = FindAnyObjectByType<ConnectionManager>();
            if (serverManager != null)
            {
                networkOpening = serverManager.doorOpening;
            }
        }

        networkOpening = Mathf.Clamp01(networkOpening);

        if (door_left != null && OpenedLeftPos != null)
        {
            door_left.position = Vector3.Lerp(ClosedLeftPos, OpenedLeftPos.position, networkOpening);
        }

        if (door_right != null && OpenedRightPos != null)
        {
            door_right.position = Vector3.Lerp(ClosedRightPos, OpenedRightPos.position, networkOpening);
        }

        if (timer_text != null)
        {
            float clousingTimer =0;

            ConnectionManager.DoorState actualState = ConnectionManager.DoorState.Closed;


            if (ClientNetworkManager.Instance != null)
            {
                clousingTimer = ClientNetworkManager.Instance.currentClousingTimer;
                actualState = ClientNetworkManager.Instance.currentDoorState;

            }
            else if (serverManager != null)
            {
                clousingTimer = serverManager.ClousingTimer;
                actualState = serverManager.doorState;

            }

            float time_left = networkOpening* clousingTimer;

            if (actualState == ConnectionManager.DoorState.Opening)
            {
                timer_text.text = "Opening";
            }
            else if (actualState == ConnectionManager.DoorState.Waiting)
            {
                timer_text.text = "Open";
            }
            else if (networkOpening <= 0.0f)
            {
                timer_text.text = "Closed";
            }           
            else
            {
                timer_text.text = Mathf.CeilToInt(time_left).ToString();
            }
        }

        
    }

   
}
