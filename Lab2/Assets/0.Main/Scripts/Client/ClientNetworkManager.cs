using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VectorGraphics;
using UnityEngine.SceneManagement;
using static MainSerializer;

public class ClientNetworkManager : MonoBehaviour
{
    //static reference (for changing scenes)
    public static ClientNetworkManager Instance { get; private set; }

    //serializer
    public BaseSerializer serializer;

    //connections for the manager to access
    ClientTCP TCPconnection;
    ClientUDP UDPconnection;

    //user/player id
    public int userId;

    //canvas ui components
    [SerializeField] TMP_Text Canvas_userList;

    //ping timer (UDP)
    float pingInterval = 1f;
    float pingTimer = 0f;

    //user list 
    public List<string> usersList = new List<string>();


    //Prefab player
    [SerializeField] private GameObject playerPrefab;

    //Control all playersPrefabs
    public Dictionary<string, GameObject> spawnedPlayers = new Dictionary<string, GameObject>();

    //ui components
    public string serverIp = "127.0.0.1";
    public int port = 9050;
    public int TCPport = 9050;
    public int UDPport = 9051;
    public string userName = "Player";
    public string text = "Type here";
    public bool autoStart = false;
    public bool isHost = false;

    //game scene components
    //timer
    // ten times per second
    float sendInterval = 1f / 10f;
    float timer = 0f;
    //cube rotation
    public float cubeRot = 0f;
    //game scene's cube
    Transform theCube;

    //all log messages
    readonly List<string> m_log = new List<string>();

    //on play --> stop full ui 
    bool isPlaying = false;

    private void Awake()
    {
        //ensure there's only one instance
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //so object is not deleted when scene changes
        DontDestroyOnLoad(gameObject);

        //get tcp component
        TCPconnection = GetComponent<ClientTCP>();

        //get udp component
        UDPconnection = GetComponent<ClientUDP>();

        //create serializer
        serializer = new ClientSerializer(this);
    }

    private void Start()
    {
        ParseCommandLine();

        if (autoStart) 
        {
            TCPconnection.StartNetwork(serverIp, userName, TCPport);
            UDPconnection.StartNetwork(serverIp, userName, UDPport);
        }
    }

    private void Update()
    {
        //send ping by UDP
        ManagePing();

        //update canvas ui
        CanvasGui();

        if(isHost && Input.GetKeyDown(KeyCode.Alpha1)) //If the host press 1 --> Puzzle is completed
        {
            UDPconnection.SendPacket(serializer.SerializeTypes(false, CommandType.StartGame));
        }

        if (isPlaying)
        {
            //apply rotation
            if(theCube != null) theCube.eulerAngles = new Vector3(0, cubeRot, 0);
            //if playing --> send cube info
            timer += Time.deltaTime;
            if (timer >= sendInterval)
            {
                //send rotation
                SendCubeRot(cubeRot);
                timer = 0f;
            }
        }
    }

    public void ManagePing()
    {
        if (UDPconnection.IsRunning)
        {
            pingTimer += Time.deltaTime;
            if(pingTimer >= pingInterval)
            {
                UDPconnection.SendPacket(serializer.SerializeTypes(true));
                pingTimer = 0f;
            }
        }
    }

    public void RegisterCube(Transform cube)
    {
        theCube = cube;
    }

    void CanvasGui()
    {
        if(Canvas_userList != null)
        {
            //display user list
            Canvas_userList.text = "User List:\n";
            foreach (var user in usersList)
            {
                if (user != null) Canvas_userList.text += (user + "\n");
            }
        }
    }

    void OnGUI()
    {
        if (!isPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            GUILayout.Label("CLIENT   port " + port);

            GUILayout.BeginHorizontal();
            if (!TCPconnection.IsRunning && !UDPconnection.IsRunning)
            {
                //editable username before connecting to server
                GUILayout.Label("Username:", GUILayout.Width(70));
                userName = GUILayout.TextField(userName, GUILayout.Width(110));
                //server ip layout
                GUILayout.Label("Server IP:", GUILayout.Width(70));
                serverIp = GUILayout.TextField(serverIp, GUILayout.Width(120));
                //connect button
                if (GUILayout.Button("Connect", GUILayout.Width(110)))
                {
                    //start both tcp and udp networks
                    TCPconnection.StartNetwork(serverIp, userName, TCPport);
                    UDPconnection.StartNetwork(serverIp, userName, UDPport);
                }
            }
            else
            {
                //username shown
                GUILayout.Label("Username: " + userName);
                //chat text UI
                text = GUILayout.TextField(text, GUILayout.Width(120));
                //send chat message to server (using TCP)
                if (GUILayout.Button("Send", GUILayout.Width(110))) TCPconnection.SendPacket(serializer.SerializeText(text));
                //if host --> play button
                if (isHost)
                {
                    if (GUILayout.Button("Play", GUILayout.Width(110)))
                    {
                        BeginPlay();
                        isPlaying = true;
                    }
                }
                //disconnect button
                if (GUILayout.Button("Disconnect", GUILayout.Width(110)))
                {
                    //disconnect both networks
                    TCPconnection.Disconnect();
                    UDPconnection.Disconnect();

                    //clear user list
                    usersList.Clear();
                }
            }

            GUILayout.EndHorizontal();

            lock (m_log)
            {
                for (int i = Mathf.Max(0, m_log.Count - 18); i < m_log.Count; i++)
                    GUILayout.Label(m_log[i]);
            }

            GUILayout.EndArea();
        }
    }

    public void BeginPlay()
    {
        if (isHost)
        {
            //send server the command to start the game
            UDPconnection.SendPacket(serializer.SerializeTypes(false, CommandType.StartGame));
        }
    }

    public void SendCubeRot(float rot)
    {
        //send current cube rotation 
        if (isHost) UDPconnection.SendPacket(serializer.SerializeCubeRot(cubeRot));
    }
    public void LoadGameScene()
    {
        //load the game scene
        SceneManager.LoadScene("Client_GameScene");

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SpawnMultiplayerPlayers;

        //make sure all players are set to playing (even if not host)
        isPlaying = true;

    }

    public void Log(string message)
    {
        Debug.Log(message);
        lock (m_log)
        {
            m_log.Add(message);
            if (m_log.Count > 100) m_log.RemoveAt(0);
        }
    }

    void ParseCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-ip" && i + 1 < args.Length) serverIp = args[++i];
            else if (args[i] == "-port" && i + 1 < args.Length) int.TryParse(args[++i], out port);
            else if (args[i] == "-name" && i + 1 < args.Length) userName = args[++i];
            else if (args[i] == "-autostart") autoStart = true;
            else if (args[i] == "-noautostart") autoStart = false;
        }
    }


    public void SpawnMultiplayerPlayers(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SpawnMultiplayerPlayers;

        float spawnX = 0f;

        for (int i = 0; i < usersList.Count; i++)
        {
            if (!string.IsNullOrEmpty(usersList[i])) 
            {
                GameObject instante_player = Instantiate(playerPrefab, new Vector3(spawnX,1f,0f), Quaternion.identity);

                //Text name
                TMPro.TextMeshProUGUI textNamePlayer = instante_player.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (textNamePlayer != null) textNamePlayer.text = usersList[i];

                //Deactivate camera and movement for other players for don't share
                if (usersList[i] != userName)
                {
                    Transform camaraRemota = instante_player.transform.Find("First Person Camera");
                    if (camaraRemota != null) camaraRemota.gameObject.SetActive(false);

                    FirstPersonMovement moverRemoto = instante_player.GetComponent<FirstPersonMovement>();
                    if (moverRemoto != null) moverRemoto.enabled = false;
                }

                spawnedPlayers[usersList[i]] = instante_player;

            }
            

            spawnX += 3f;
        }

    }

}
