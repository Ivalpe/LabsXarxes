using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Mesh Transforms")]
    public Transform door_left;
    public Transform door_right;

    private Vector3 initialLeftPos;
    private Vector3 initialRightPos;

    ConnectionManager connectionManager;

    void Start()
    {

        if (door_left != null && door_right != null)
        {
            initialLeftPos = door_left.position;
            initialRightPos = door_right.position;
        }


    }

    void Update()
    {
        if (ClientNetworkManager.Instance != null)
        {
            float networkOpening = ClientNetworkManager.Instance.currentDoorOpening;
            UpdateMove(networkOpening);
        }
    }

    void UpdateMove(float opening)
    {
        if (door_left == null || door_right == null) return;

        door_left.position = new Vector3(initialLeftPos.x + opening, initialLeftPos.y, initialLeftPos.z);
        door_right.position = new Vector3(initialRightPos.x - opening, initialRightPos.y, initialRightPos.z);
    }
}
