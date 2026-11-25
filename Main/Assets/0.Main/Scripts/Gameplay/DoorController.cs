using UnityEngine;
using TMPro;
using System.IO;
using System.Collections;


public class DoorController : NetObject
{
    [Header("Door Mesh Transforms")]
    public Transform door_left;
    public Transform door_right;

    //Limit for the opening of the door
    public Transform OpenedLeftPos;
    public Transform OpenedRightPos;

    //Limit closed Door
    public Transform ClosingLeftPos;
    public Transform ClosingRightPos;

    private Vector3 ClosedLeftPos;
    private Vector3 ClosedRightPos;

    //ConnectionManager connectionManager;

    public TextMeshPro timer_text;
    private ConnectionManager serverManager;
    public Light doorLight;

    void Start()
    {
        if (serverManager == null) serverManager = FindAnyObjectByType<ConnectionManager>();

        if (door_left != null && door_right != null)
        {
            ClosedLeftPos = door_left.position;
            ClosedRightPos = door_right.position;
        }
        //doorLight=GetComponent<Light>();
        doorLight.intensity = 0.0f;
    }
    
    //Slowly closes the door until its fully closed from the previously setted up pos
    void Update()
    {
        float networkOpening = 0.0f;
        float clousingTimer = 0.0f;
        ConnectionManager.DoorState actualState = ConnectionManager.DoorState.Closed;

        //Get values from the client or Server
        if (ClientNetworkManager.Instance != null)
        {
            networkOpening = ClientNetworkManager.Instance.currentDoorOpening;
            clousingTimer = ClientNetworkManager.Instance.currentClousingTimer;
            actualState = ClientNetworkManager.Instance.currentDoorState;
        }
        else
        {
            if(serverManager==null) serverManager = FindAnyObjectByType<ConnectionManager>();
            if (serverManager != null)
            {
                networkOpening = serverManager.doorOpening;
                clousingTimer = serverManager.ClousingTimer;
                actualState = serverManager.doorState;
            }
        }

        networkOpening = Mathf.Clamp01(networkOpening);
        float time_left = networkOpening * clousingTimer;

        //Move door
        if ((actualState == ConnectionManager.DoorState.Closing && time_left <= 1.0f) || actualState == ConnectionManager.DoorState.Closed) //When 1 second, closed inmediatelty
        {
            if (door_left != null)
            {
                door_left.position = ClosedLeftPos;
            }

            if (door_right != null)
            {
                door_right.position = ClosedRightPos;
            }
            Flicker();
        }
        else
        {
            if (door_left != null && OpenedLeftPos != null && ClosingLeftPos!=null)
            {
                door_left.position = Vector3.Lerp(ClosingLeftPos.position, OpenedLeftPos.position, networkOpening);
            }

            if (door_right != null && OpenedRightPos != null && ClosingRightPos!=null)
            {
                door_right.position = Vector3.Lerp(ClosingRightPos.position, OpenedRightPos.position, networkOpening);
            }
        }


        if (timer_text != null)
        {
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


            if (actualState == ConnectionManager.DoorState.Opening)
            {
                timer_text.text = "OPENING";
            }
            else if (actualState == ConnectionManager.DoorState.Waiting)
            {
                timer_text.text = "OPEN";
            }
            else if (networkOpening <= 0.0f)
            {
                timer_text.text = "CLOSED";
            }
            else
            {
                timer_text.text = Mathf.CeilToInt(time_left).ToString();
            }
        }

        
    }

    public override void Write(BinaryWriter w)
    {
        if (serverManager == null) serverManager = FindAnyObjectByType<ConnectionManager>();
        if (serverManager != null)
        {
            w.Write(serverManager.doorOpening);
            w.Write(serverManager.ClousingTimer);
            w.Write((byte)serverManager.doorState);
        }
    }

    public override void Read(BinaryReader r)
    {
        float opening = r.ReadSingle();
        float timer = r.ReadSingle();
        ConnectionManager.DoorState state = (ConnectionManager.DoorState)r.ReadByte();
        //Debug.Log($"[CLIENT DOOR] opening={opening}, state={state}");

        if (ClientNetworkManager.Instance != null)
        {
            ClientNetworkManager.Instance.currentDoorOpening = opening;
            ClientNetworkManager.Instance.currentClousingTimer = timer;
            ClientNetworkManager.Instance.currentDoorState = state;
        }
    }

    private void Flicker()
    {
        time();
        doorLight.intensity = Random.Range(1f, 1.5f);
       
    }

    IEnumerator time()
    {
        yield return new WaitForSeconds(2);
    }
     
}
