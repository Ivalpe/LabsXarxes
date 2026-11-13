using UnityEngine;

public abstract class NetworkManager : MonoBehaviour
{
    public abstract void SendMessage(string msg);
}
