using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.VectorGraphics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MainSerializer;


public class ClientNetworkManager : MonoBehaviour
{
    //static reference (for changing scenes)
    public static ClientNetworkManager Instance { get; private set; }

    //serializer
    public BaseSerializer serializer;

    //connections for the manager to access
    public ClientTCP TCPconnection;
    public ClientUDP UDPconnection;

    //user/player id
    public int userId;

    //canvas ui components
    [SerializeField] TMP_Text Canvas_userList;

    //ping timer (UDP)jump
    float pingInterval = 1f;
    float pingTimer = 0f;

    //Vector3 targetPos = Vector3.zero;
    //Quaternion targetRot = Quaternion.identity;
    byte playerColorIndex;
    Pipe[,] pipeMap;
    //GameObject[,] tileObj;

    //user list 
    public List<string> usersList = new List<string>();

    public System.Action<byte> OnPlayerRegistered;
    public System.Action OnAllPlayersSpawned;

    //public System.Action<Pipe> OnTileRotated;

    //list of colors
    private readonly List<Color> playerColors = new List<Color>
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    //List for initial user spawning
    public List<PlayerInfo> playersToSpawn = new List<PlayerInfo>();

    //Prefab player
    [SerializeField] private GameObject playerPrefab;
    
    //Control all playersPrefabs
    public Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();

    //ui components
    public string serverIp = "127.0.0.1";
    public int port = 9050;
    public int TCPport = 9050;
    public int UDPport = 9051;
    public string userName = "Player";
    public string text = "Type here";
    public bool autoStart = false;
    public bool isHost = false;

    //variable door
    public float currentDoorOpening = 0.0f;

    //game scene components
    //timer
    // ten times per second
    float sendInterval = 1f / 10f;
    float timer = 0f;
    //cube rotation
    public float cubeRot = 0f;
    //game scene's cube
    Transform theCube;

    //player component
    Player playerComponent;

    //all log messages
    readonly List<string> m_log = new List<string>();

    //on play --> stop full ui 
    bool isPlaying = false;

    private void OnEnable()
    {
        OnPlayerRegistered += SetClientPlayerColor;
        Player.OnTileRotated += SendTileRotation;
       
        
    }

    private void OnDisable()
    {
        OnPlayerRegistered -= SetClientPlayerColor;
        Player.OnTileRotated -= SendTileRotation;

    }

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

        if (isHost && Input.GetKeyDown(KeyCode.Alpha1)) //If the host press 1 --> Puzzle is completed
        {
            UDPconnection.SendPacket(serializer.SerializeTypes(false, CommandType.StartGame));
        }

        if (isPlaying)
        {
            //apply rotation
            if (theCube != null) theCube.eulerAngles = new Vector3(0, cubeRot, 0);
            //if playing --> send cube info
            timer += Time.deltaTime;
            if (timer >= sendInterval)
            {
                //send rotation
                SendCubeRot(cubeRot);
                timer = 0f;
            }
        }

        ////smoothly apply transform (done here instead of OnWorldUpdate to avoid jitter)
        //if (spawnedPlayers.TryGetValue(userId, out var player))
        //{

        //    Vector3.MoveTowards(player.transform.position, targetPos, Time.deltaTime);
        //    Vector3.RotateTowards(player.transform.rotation.eulerAngles, targetRot.eulerAngles, Time.deltaTime, 0.0f);


        //}
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

    public void OnWorldUpdate(BinaryReader reader) //received from connectionmanager FixedUpdate
    {
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            //get id
            int id = reader.ReadInt32();

            //get position and turn to vector
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Vector3 pos = new Vector3(x, y, z);
            //get rotation value
            float rotY = reader.ReadSingle();


            //float yVel = reader.ReadSingle();


            if (spawnedPlayers.TryGetValue(id, out var player))
            {
                //no need to get the rb here, since the server already calculated positions with rb.MovePosition and stored them in playerinfo
                player.transform.position = pos;
                if (id != userId) player.transform.rotation = Quaternion.Euler(0, rotY, 0);

            }
        }

        if (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            currentDoorOpening = reader.ReadSingle();
        }
    }

    public void OnBoardCreated(BinaryReader reader)
    {
        PipeControl boardScript = FindAnyObjectByType<PipeControl>();

        int rows = reader.ReadInt32();
        int cols = reader.ReadInt32();

        pipeMap = new Pipe[rows, cols];
        

        for(int i = 0; i < rows; ++i)
        {
            for(int j = 0; j < cols; ++j)
            {
                Pipe p = new Pipe();
                

                p.type = reader.ReadInt32();
                if(p.type == 0)
                {
                    p.exit1 = 1;
                    p.exit2 = 2;
                    p.prefab = boardScript.cornerTile;
                }
                else if(p.type == 1)
                {
                    p.exit1 = 1;
                    p.exit2 = 3;
                    p.prefab = boardScript.straightTile;
                }

                float boardPosX = reader.ReadSingle();
                float boardPosY = reader.ReadSingle();
                p.rotIndex = reader.ReadInt32();
                
                float r = reader.ReadSingle();
                float g = reader.ReadSingle();
                float b = reader.ReadSingle();
                float a = reader.ReadSingle();
                p.tileColor = new Color(r, g, b, a);

                p.boardPos = new Vector3(boardPosX, boardPosY, 0);
               

                p.tileGO = Instantiate(p.prefab, boardScript.gameObject.transform);
                p.tileGO.transform.localPosition = p.boardPos;
                p.tileGO.transform.localRotation = Quaternion.Euler(0f, 0f, p.rotIndex * 90f);
                //p.tileGO.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                //p.tileGO.GetComponent<Renderer>().material.SetColor("_Emission", Color.black);
                p.tileGO.GetComponent<Renderer>().material.SetColor("_Color", p.tileColor);

                if (p.tileGO.TryGetComponent<TubeTile>(out TubeTile tileScript))
                {
                    tileScript.xIndex = i;
                    tileScript.yIndex = j;
                    tileScript.tileColor = p.tileColor;

                    if (tileScript.mat == null)
                    {
                        if (p.tileGO.TryGetComponent<Renderer>(out Renderer renderer))
                        {
                            
                            tileScript.mat = renderer.material;
                            tileScript.mat.SetColor("_Color", tileScript.tileColor);
                        }

                    }
                }

                pipeMap[i, j] = p;
            }
        }

        boardScript.pipeMap = pipeMap; 
        //has to be reassigned because the GO's info was filled on the server's boardScript, not this one.
    }

    public void OnBoardUpdate(BinaryReader reader)
    {
        int xIndex = reader.ReadInt32(); 
        int yIndex = reader.ReadInt32(); 
        

        bool found = false;


        for(int i = 0; i < pipeMap.GetLength(0); ++i) 
        {
            for(int j = 0; j < pipeMap.GetLength(1);  ++j)
            {
                if (i == xIndex && j == yIndex)
                {
                    found = true;
                    break;
                }
                
            }
        }

        if (found)
        {
            if(pipeMap[xIndex, yIndex].tileGO.TryGetComponent<TubeTile>(out TubeTile tileScript))
            {
                tileScript.StartRotation(pipeMap[xIndex, yIndex]);
            }
        }

        PipeControl boardScript = GameObject.FindAnyObjectByType<PipeControl>();
        if (boardScript) boardScript.pipeMap = pipeMap;
        else Debug.Log("BoardScript not found when updating board!");
    }

    public void RegisterCube(Transform cube)
    {
        theCube = cube;
    }

    public void RegisterPlayerScript(Player comp)
    {
        playerComponent = comp;
        OnPlayerRegistered?.Invoke(playerColorIndex);
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
                    playersToSpawn.Clear();
                    spawnedPlayers.Clear();
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
    public void LoadGameScene(BinaryReader reader)
    {
        //load the game scene
        SceneManager.LoadScene("Client_GameScene");

        //get the list of players to spawn (with positions, names, colors...)
        HandleToSpawnList(reader);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SpawnMultiplayerPlayers;
        

        //make sure all players are set to playing (even if not host)
        isPlaying = true;

    }

    public void HandleToSpawnList(BinaryReader reader)
    {
        int count = reader.ReadInt32();

        if (count <= 0) return;

        playersToSpawn.Clear();
        spawnedPlayers.Clear();

        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            Vector3 pos = new Vector3(x, y, z);
            float rotY = reader.ReadSingle();
            Vector3 rot = new Vector3(0, rotY, 0);
            //float yVel = reader.ReadSingle();
            byte colorIndex = reader.ReadByte();


            PlayerInfo player = new PlayerInfo(id, name, pos, rot, /*yVel, */colorIndex);
            playersToSpawn.Add(player);
        }
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

        foreach (var player in playersToSpawn)
        {
            Quaternion rotation = Quaternion.Euler(player.rotation);
            GameObject instancePlayer = Instantiate(playerPrefab, player.position, rotation);

            //Text name
            TMPro.TextMeshProUGUI textNamePlayer = instancePlayer.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textNamePlayer != null) textNamePlayer.text = player.playerName;

            //material color
            var renderer = instancePlayer.GetComponentInChildren<Renderer>();
            Color color = playerColors[player.colorIndex];
            if(renderer != null) renderer.material.SetColor("_Color", color);
            Rigidbody rb = instancePlayer.GetComponent<Rigidbody>();
            if (rb)
                rb.isKinematic = true; //otherwise you can't use rb.MovePosition or rb.MoveRotation

            //check if the user is this client
            if (player.id != userId)
            {
                Transform camaraRemota = instancePlayer.transform.Find("First Person Camera");
                if (camaraRemota != null) camaraRemota.gameObject.SetActive(false);

                PlayerInput playerInput = instancePlayer.GetComponent<PlayerInput>();
                if (playerInput != null) playerInput.enabled = false;

                Player playerScript = instancePlayer.GetComponent<Player>();
                if (playerScript != null) playerScript.enabled = false;
            }
            else
            {
                //assign color to player script

                playerColorIndex = player.colorIndex; //caching it to use in an action
                //OnPlayerRegistered += SetClientPlayerColor;
                
            }

            //add to dictionary
            spawnedPlayers[player.id] = instancePlayer;
        }

        OnAllPlayersSpawned?.Invoke();
        
    }

    void SetClientPlayerColor(byte index)
    {
        
         if (playerComponent != null)
             playerComponent.SetPlayerColor(index);
    }

    void SendTileRotation(Pipe p)
    {
        byte[] tilePacket = serializer.SerializeTile(p);
        TCPconnection.SendPacket(tilePacket);
    }


}
