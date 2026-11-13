using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public abstract class NetworkManager : MonoBehaviour
{
    public GameManager gameManager;
    public void SetManagerInstance(GameManager manager)
    {
        gameManager = manager;
    }

    public abstract void InstantiateConnection(string name, string ip = null);
    public abstract void SendChatMessage(string msg);
    public abstract List<string> GetUserList();
    public virtual void StartGame() { }
}
