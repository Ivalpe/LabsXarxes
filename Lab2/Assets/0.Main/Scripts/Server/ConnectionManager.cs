using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.XR;
using static MainSerializer;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;

    

    public Dictionary<int, GameObject> spawnedPlayers = new Dictionary<int, GameObject>();


    public float jumpStr = 1000f;
    public Connection ctpc;
    public Connection cudp;

    public System.Action OnServerPlayersSpawned;

    private readonly List<Color> playerColors = new List<Color>
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    Dictionary<Color, Color> playerTileColorPairs = new Dictionary<Color, Color>
    {
        {Color.red, new Color(1f, 0.6f, 0.6f, 1f) },
        {Color.blue, new Color(0.4f, 1f, 1f, 1f)},
        {Color.green, new Color(0.6f, 1f, 0.5f, 1f) },
        {Color.yellow, new Color(1f, 0.9f, 0.4f, 1f) },
        {Color.magenta, new Color(1f, 0.5f, 0.9f, 1.0f)},
        {Color.cyan, new Color(0.6f, 0.9f, 0.8f, 1f) }
    };

    //serializer
    public BaseSerializer serializer;

    public int portTcp = 9050;
    public int portUdp = 9051;
    public string serverName = "MyServer";
    public bool autoStart = true;

    bool changedScene = false;
    readonly List<string> m_log = new List<string>();

    //gameplay variables
    public bool isPlaying = false;
    public Dictionary<int, PlayerInfo> playerInfoDic = new Dictionary<int, PlayerInfo>();
    public float speed = 5;
    public float gravityScale = 1;
    public float maxJumpHeight = 3;

    Collider floorCollider;


    //door variables

    public int doorType = 1;
    public float doorOpening = 0.0f;
    private float maxOpening = 1.0f;
    private float speedOpen = 5.0f;
    private float totalCloseTime = 60.0f;
    private float timeRemaining = 60.0f;

    private void OnEnable()
    {
        PipeControl.OnBoardCreated += ApplyTileColors;
    }

    private void OnDisable()
    {
        PipeControl.OnBoardCreated -= ApplyTileColors;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        doorType = 1;
        ParseCommandLine();

        if (autoStart)
        {
            ctpc.StartNetwork();
            cudp.StartNetwork();
        }
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        //set tpc connection type
        ctpc.SetType(Connection.ConnectionType.TCP);
        ctpc.SetPort(portTcp);

        //set udp connection type
        cudp.SetType(Connection.ConnectionType.UDP);
        cudp.SetPort(portUdp);

        //create serializer
        serializer = new ServerSerializer(this);
    }

    private void FixedUpdate()
    {
        if (isPlaying)
        {

            float dt = Time.fixedDeltaTime;

            
            //update for each player
            foreach(var entry in playerInfoDic)
            {                
                PlayerInfo player = entry.Value;
                if (player == null) continue;

                if (!spawnedPlayers.TryGetValue(player.id, out GameObject playerObj) || playerObj == null) continue;
                if (!playerObj.TryGetComponent(out Rigidbody rb)) continue;
                
                bool isGrounded = IsGrounded(playerObj);

                //calculate move direction
                Quaternion yawRot = Quaternion.Euler(0, player.input.yawRot, 0);
                Vector3 moveDir = (yawRot * Vector3.forward * player.input.vertical) +
                                  (yawRot * Vector3.right * player.input.horizontal);

                Vector3 vel = rb.linearVelocity;
                vel.x = moveDir.x * speed;
                vel.z = moveDir.z * speed;
                //vel.y while not jumping is already computed w physics & gravity

               

                if (player.input.jump && isGrounded)
                {     
                    float jumpStr = Mathf.Sqrt(-2 * (Physics.gravity.y * gravityScale) * maxJumpHeight);
                    rb.AddForce(Vector3.up * jumpStr, ForceMode.VelocityChange); 
                    //forcemode.force is meant to be added overtime, it would only jump a tiny amount.
                    //forcemode.velchange, on the contrary, is instantaneous and mass-independent
                    player.input.jump = false; //consume the jump, like an event
                    
                }

                rb.linearVelocity = vel;
                rb.MoveRotation(Quaternion.Euler(0, player.input.yawRot, 0)); 
                //rb.freezerotation is set to true so contrary to the position, rb.rotation does need manual setup
                

                //assign rb position and rotation to playerinfo since it adapted to object collisions
                player.position = rb.position;
                player.rotation = rb.rotation.eulerAngles;
                //player.yVelocity = rb.linearVelocity.y;

                //Debug.Log($"rb.position.y = {rb.position.y}, isGrounded? = {isGrounded}, player.yVelocity = {player.yVelocity} ");
               
               
               
            }

            if (doorType == 1) // Abriendo rápido
            {
                doorOpening += speedOpen * dt;
                if (doorOpening >= maxOpening)
                {
                    doorOpening = maxOpening;
                    timeRemaining = totalCloseTime;
                    doorType = 2; // Pasa a cerrar lento
                }
            }
            else if (doorType == 2) // Cerrando lento
            {
                timeRemaining -= dt;
                float progressPercentage = timeRemaining / totalCloseTime;
                doorOpening = maxOpening * progressPercentage;

                if (timeRemaining <= 0 || doorOpening <= 0.0f)
                {
                    timeRemaining = 0;
                    doorOpening = 0.0f;
                    doorType = 0; // Cerrada del todo
                    //isDoorClosed = true;
                }
            }

            //send world info to all users
            List<PlayerInfo> playerList = new List<PlayerInfo>(playerInfoDic.Values);
            byte[] worldPacket = serializer.SerializeWorldPos(playerList, false, doorOpening);
            cudp.UDPSendToAllClients(worldPacket);

        }
    }

    public void SetUpGameScene()
    {
        //load the game scene
        SceneManager.LoadScene("Client_GameScene");

        DontDestroyOnLoad(Camera.main);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += SpawnServerPlayers; //call only when scene is loaded
        

        isPlaying = true;
    }

    public void SpawnServerPlayers(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SpawnServerPlayers;


        List<PlayerInfo> playersToSpawn = new List<PlayerInfo>(playerInfoDic.Values);

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
            if (renderer != null) renderer.material.SetColor("_Color", color);

            Rigidbody rb = instancePlayer.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false; //make them dynamic, the server is the one to apply physics
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation; //will need manual setup in fixedupdate
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            }
            Transform camaraRemota = instancePlayer.transform.Find("First Person Camera");
            if (camaraRemota != null) camaraRemota.gameObject.SetActive(false);

            PlayerInput playerInput = instancePlayer.GetComponent<PlayerInput>();
            if (playerInput != null) playerInput.enabled = false;

            Player playerScript = instancePlayer.GetComponent<Player>();
            if (playerScript != null) playerScript.enabled = false;

            //add to dictionary
            spawnedPlayers[player.id] = instancePlayer;
        }



    }


    bool IsGrounded(GameObject playerObj)
    { 
        
        if (!playerObj) return false;
        if (!playerObj.TryGetComponent<Collider>(out Collider pCol)) return false;
        
        
        Vector3 origin = pCol.bounds.center - new Vector3(0, pCol.bounds.extents.y - 0.1f, 0);
        //slightly offset because starting at the exact bounds can cause errors due to floating point precision
        float raycastDist = 0.2f;
       
        Debug.DrawRay(origin, Vector3.down * raycastDist, Color.red);

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore); 
        // get ALL objects the player intersects (and ignore trigger colliders)

        foreach (var hit in hits)
        {
            // Ignore the player's own collider
            if (hit.collider != null && !hit.collider.transform.IsChildOf(playerObj.transform))
            {
                return true;
            }
        }
       
        
        return false;
    }

    void ParseCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length) int.TryParse(args[++i], out portTcp);
            else if (args[i] == "-name" && i + 1 < args.Length) serverName = args[++i];
            else if (args[i] == "-autostart") autoStart = true;
            else if (args[i] == "-noautostart") autoStart = false;
        }
    }
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
        GUILayout.Label("SERVER   name " + serverName);

        if (!ctpc.IsRunning && !cudp.IsRunning)
        {
            if (GUILayout.Button("Start server", GUILayout.Width(110)))
            {
                ctpc.StartNetwork();
                cudp.StartNetwork();
            }
        }
        else if (GUILayout.Button("Stop", GUILayout.Width(110)))
        {
            ctpc.Disconnect();
            cudp.Disconnect();
        }

        lock (m_log)
            for (int i = Mathf.Max(0, m_log.Count - 18); i < m_log.Count; i++)
                GUILayout.Label(m_log[i]);

        GUILayout.EndArea();
    }
    public void Log(string message)
    {
        Debug.Log(message);
        lock (m_log) { m_log.Add(message); if (m_log.Count > 100) m_log.RemoveAt(0); }
    }


    public void ResetDoorState()
    {
       doorType = 1;
       doorOpening = 0.0f;
       timeRemaining = 60.0f;
    }

    public void ApplyTileColors(Pipe[,] pipeMap)
    {
        List<Color> activePlayerColors = new List<Color>();
        foreach(Pipe p in pipeMap)
        {
            Color[] tileColors;
            if (spawnedPlayers.Count < 2)
            {
                tileColors = new Color[2]
                {
                   playerTileColorPairs.Values.ToArray()[0],
                   playerTileColorPairs.Values.ToArray()[1]
                };
            }
            else
            {
                foreach (PlayerInfo pInfo in playerInfoDic.Values.ToArray())
                { 
                    activePlayerColors.Add(pInfo.color);
                }

                tileColors = playerTileColorPairs.Values.ToArray();
                
            }



            if (tileColors.Length != 0)
            {
                int r = UnityEngine.Random.Range(0, tileColors.Length);
                if (playerTileColorPairs.ContainsKey(tileColors[r]))
                {
                    p.tileColor = playerTileColorPairs[tileColors[r]];
                }
                else
                {
                    p.tileColor = tileColors[r];
                }
                
            }
        }

        
        
    }
}
