using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Mesh Transforms")]
    public Transform door_left;
    public Transform door_right;

    public Transform OpenedLeftPos;
    public Transform OpenedRightPos;

    private Vector3 ClosedLeftPos;
    private Vector3 ClosedRightPos;

    ConnectionManager connectionManager;

    void Start()
    {

        if (door_left != null && door_right != null)
        {
            ClosedLeftPos = door_left.position;
            ClosedRightPos = door_right.position;
        }


    }

    void Update()
    {
        if (ClientNetworkManager.Instance != null)
        {
            float networkOpening = ClientNetworkManager.Instance.currentDoorOpening;

            networkOpening = Mathf.Clamp01(networkOpening);

            if (door_left != null && OpenedLeftPos != null)
            {
                door_left.position = Vector3.Lerp(ClosedLeftPos, OpenedLeftPos.position, networkOpening);
            }

            if (door_right != null && OpenedRightPos != null)
            {
                door_right.position = Vector3.Lerp(ClosedRightPos, OpenedRightPos.position, networkOpening);
            }

        }
    }

   
}
