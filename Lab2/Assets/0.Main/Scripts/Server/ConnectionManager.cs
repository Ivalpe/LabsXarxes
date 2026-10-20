using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MainSerializer;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;
    
    public Connection ctpc;
    public Connection cudp;

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
    float velocity = 0;
    public float gravityScale = 1;
    public float maxJumpHeight = 3;

    bool isPlayerGrounded = true;

    float maxRayCastDist = 0.15f;

    Collider floorCollider;


    //door variables

    public int doorType = 1;
    public float doorOpening = 0.0f;
    private float maxOpening = 5.0f;
    private float speedOpen = 5.0f;
    private float totalCloseTime = 60.0f;
    private float timeRemaining = 60.0f;


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

            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Client_GameScene" && !changedScene)
            {
                SceneManager.LoadScene("Client_GameScene"); //we need to change the scene in order to perform raycasts from the server side
                changedScene = true;
            }
            float dt = Time.fixedDeltaTime;

            
            //update for each player
            foreach(var entry in playerInfoDic)
            {
                bool isGrounded = false;
                
                PlayerInfo player = entry.Value;
                if (player == null) continue;

                

                //calculate move direction
                Quaternion yawRot = Quaternion.Euler(0, player.input.yawRot, 0);
                Vector3 moveDir = (yawRot * Vector3.forward * player.input.vertical) +
                                  (yawRot * Vector3.right * player.input.horizontal);

                //add move to position
                player.position += moveDir * speed * dt;
                //set rotation
                player.rotation = new Vector3(0, player.input.yawRot, 0);

                player.yVelocity += Physics.gravity.y * gravityScale * dt;

                
                if (IsGrounded(player, out floorCollider) && player.yVelocity < 0)
                {
                    player.yVelocity = 0;
                    player.position.y = floorCollider.ClosestPoint(player.position).y;

                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }

                if (player.input.jump && isGrounded)
                {

                    player.yVelocity = Mathf.Sqrt(-2 * (Physics.gravity.y * gravityScale) * maxJumpHeight);
                    //player.input.jump = false;
                    //player.input.grounded = false;
                    //isPlayerGrounded = IsGrounded(player, out Collider col); //
                }

                player.position += (new Vector3(0, player.yVelocity, 0 ) * dt);
                
            }

            if (doorType == 1) // Abriendo rápido
            {
                doorOpening += speedOpen * dt;
                if (doorOpening >= maxOpening)
                {
                    doorOpening = maxOpening;
                    doorType = 2; // Pasa a cerrar lento
                }
            }
            else if (doorType == 2) // Cerrando lento
            {
                timeRemaining -= dt;
                float progressPercentage = timeRemaining / totalCloseTime;
                doorOpening = maxOpening * progressPercentage;

                if (timeRemaining <= 0)
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

    bool IsGrounded(PlayerInfo player, out Collider floorCollider)
    {

        Vector3 pos = new Vector3(player.position.x, player.position.y, player.position.z);

        //Raising origin slightly so the ray starts INSIDE the body and avoids ground intersection
        float originOffset = 0.2f;
        Vector3 raycastOrigin = pos + Vector3.up * originOffset;

        //raycast dist = offset + ground check range
        float distanceThreshold = 0.5f;
        float raycastDistance = distanceThreshold + originOffset;
        Debug.DrawRay(raycastOrigin, Vector3.down, Color.red);

        if (Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, raycastDistance))
        {
            // Ignore hitting the player's own collider if raycast originates inside it
            if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
            {
                floorCollider = hit.collider;   
                return true;
            }
        }
        floorCollider = null;
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
}
