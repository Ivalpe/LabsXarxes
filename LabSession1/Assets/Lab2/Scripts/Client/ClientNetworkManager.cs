using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientNetworkManager : MonoBehaviour
{
    //connections for the manager to access
    ClientTCP TCPconnection;
    ClientUDP UDPconnection;

    //ui components
    public string serverIp = "127.0.0.1";
    public int port = 9050;
    public string userName = "Player";
    public string text = "Type here";
    public bool autoStart = false;

    //all log messages
    readonly List<string> m_log = new List<string>();

    //on play --> stop full ui 
    bool isPlaying = false;

    private void Awake()
    {
        //so object is not deleted when scene changes
        DontDestroyOnLoad(gameObject);

        //get tcp component
        TCPconnection = GetComponent<ClientTCP>();

        //get udp component
        UDPconnection = GetComponent<ClientUDP>();
    }

    private void Start()
    {
        ParseCommandLine();

        if (autoStart) 
        {
            TCPconnection.StartNetwork(serverIp, userName);
            UDPconnection.StartNetwork(serverIp, userName);
        }
    }

    void OnGUI()
    {
        if (!isPlaying)
        {
            GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
            GUILayout.Label("CLIENT   port " + port);

            GUILayout.BeginHorizontal();
            if (!TCPconnection.IsRunning)
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
                    TCPconnection.StartNetwork(serverIp, userName);
                    UDPconnection.StartNetwork(serverIp, userName);
                }
            }
            else
            {
                //username shown
                GUILayout.Label("Username: " + userName);
                //chat text UI
                text = GUILayout.TextField(text, GUILayout.Width(120));
                //send chat message to server (using TCP)
                if (GUILayout.Button("Send", GUILayout.Width(110))) TCPconnection.SendString(text);
                //if host --> play button
                if (TCPconnection.isClientHost())
                {
                    if (GUILayout.Button("Play", GUILayout.Width(110)))
                    {
                        TCPconnection.BeginPlay();
                        isPlaying = true;
                    }
                }
                //disconnect button
                if (GUILayout.Button("Disconnect", GUILayout.Width(110)))
                {
                    //disconnect both networks
                    TCPconnection.Disconnect();
                    UDPconnection.Disconnect();
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
}
