using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    NetworkManager networkManager;
    string userName;

    //MAIN MENU
    [SerializeField] TMP_InputField createUserNameInput;
    [SerializeField] TMP_InputField joinUserNameInput;
    [SerializeField] TMP_InputField ipInput;

    //WAITING ROOM
    bool waitingRoomActive = false;
    //chat
    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInput;
    //user list
    [SerializeField] TMP_Text userList;

    public void CreateRoom() {
        userName = createUserNameInput.text;
    }

    public void JoinRoom() {
        userName = joinUserNameInput.text;
        string ip = ipInput.text;
        networkManager = new ClientNetworkManager(userName, ip);
    }

    private void Update()
    {
        if (waitingRoomActive)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendChatMessage(chatInput.text, )
            }
        }
    }

    public void SendChatMessage()
    {

    }
}
