using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    NetworkManager networkManager;
    string userName;
    bool isHost = false;

    //NETWORK MANAGERS
    [SerializeField] ConnectionManager serverPrefab;
    [SerializeField] ClientNetworkManager clientPrefab;

    //MAIN MENU
    [SerializeField] TMP_InputField createUserNameInput;
    [SerializeField] TMP_InputField joinUserNameInput;
    [SerializeField] TMP_InputField ipInput;

    //WAITING ROOM
    bool waitingRoomActive = false;
    [SerializeField] Button startGameButton;
    //chat
    [SerializeField] ChatMessage chatMessagePrefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInput;
    //user list
    [SerializeField] TMP_Text userList;

    private void Awake()
    {
        //so object is not deleted when scene changes
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (ipInput != null && string.IsNullOrEmpty(ipInput.text))
        {
            ipInput.text = "127.0.0.1";
        }
    }

    public void CreateRoom() {
        userName = createUserNameInput.text;
        isHost = true;
        networkManager = Instantiate(serverPrefab);
        networkManager.SetManagerInstance(this);
        networkManager.InstantiateConnection(userName);
        waitingRoomActive = true;
    }

    public void JoinRoom() {
        userName = joinUserNameInput.text;
        string ip = ipInput.text;
        networkManager = Instantiate(clientPrefab);
        networkManager.SetManagerInstance(this);
        networkManager.InstantiateConnection(userName, ip);
        waitingRoomActive = true;
    }

    private void Update()
    {
        if (waitingRoomActive)
        {
            //send message
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendChatMessage(chatInput.text);
                chatInput.text = "";
            }

            //update user list
            List<string> nameList = networkManager.GetUserList();
            userList.text = "";
            foreach (var user in nameList)
            {
                if (user != null) userList.text += (user + "\n");
            }
        }
    }

    public void SendChatMessage(string message)
    {
        //check if message is empty
        if (string.IsNullOrEmpty(message)) return;

        //string fullMsg = userName + ": " + message;
        networkManager.SendChatMessage(message);
    }

    public void ReceiveChatMessage(string msg, byte color)
    {
        if (waitingRoomActive)
        {
            ChatMessage cm = Instantiate(chatMessagePrefab, chatContent.transform);
            cm.SetText(msg, color);
        }
    }

    public void StartGameClick()
    {
        if (isHost)
        {
            waitingRoomActive = false;
            networkManager.StartGame();
        }
    }
}
