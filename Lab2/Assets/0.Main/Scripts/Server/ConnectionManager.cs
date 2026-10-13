using System;
using System.Collections.Generic;
using UnityEngine;
using static MainSerializer;

public class ConnectionManager : MonoBehaviour
{

    public Connection ctpc;
    public Connection cudp;

    //serializer
    public BaseSerializer serializer;

    public int portTcp = 9050;
    public int portUdp = 9051;
    public string serverName = "MyServer";
    public bool autoStart = true;
    readonly List<string> m_log = new List<string>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
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
