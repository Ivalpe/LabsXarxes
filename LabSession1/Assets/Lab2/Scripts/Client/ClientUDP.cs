using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class ClientUDP : MonoBehaviour
{
    ClientNetworkManager networkManager;
    
    public string serverIp = "127.0.0.1";
    public int port = 9050;
    public string userName = "Player";

    // =============================================================================================
    //  GIVEN CODE - nothing to modify until the TODO area
    // =============================================================================================

    const int MaxPacketSize = 64 * 1024;

    Socket m_socket;                                          // the only socket
    EndPoint m_serverEndPoint;                                // where your messages go
    readonly List<Thread> m_threads = new List<Thread>();
    readonly ConcurrentQueue<byte[]> m_inbox = new ConcurrentQueue<byte[]>();

    readonly HashSet<int> m_warned = new HashSet<int>();
    volatile bool m_running;

    public bool IsRunning { get { return m_running; } }

    void Start()
    {
        Application.runInBackground = true;
        networkManager = GetComponent<ClientNetworkManager>();
    }

    public void StartNetwork(string ip, string name, int portToUse)
    {
        serverIp = ip;
        userName = name;
        port = portToUse;
        if (m_running) return;
        m_running = true;
        StartThread(ClientThread);
    }

    public void Disconnect()
    {
        if (!m_running) return;
        m_running = false;

        if (m_socket != null) { try { m_socket.Close(); } catch { } m_socket = null; }

        Thread[] threads;
        lock (m_threads) { threads = m_threads.ToArray(); m_threads.Clear(); }
        foreach (Thread t in threads) if (t != Thread.CurrentThread) t.Join(500);

        Log("[CLIENT] Stopped");
    }

    void OnDestroy() { Disconnect(); }

    void Update()
    {
        byte[] data;
        while (m_inbox.TryDequeue(out data))
            OnPacketReceived(data);
    }

    void ClientThread()
    {
        m_socket = StartClient();                                                    // TODO 1
        if (m_socket == null) { WarnTodo(1, "StartClient() returned null"); return; }

        m_serverEndPoint = ServerEndPoint();                                         // TODO 2
        if (m_serverEndPoint == null) { WarnTodo(2, "ServerEndPoint() returned null"); return; }

        Log("[CLIENT] Ready, server is " + m_serverEndPoint);

        OnStarted();                                                                 // TODO 3
        ReceiveLoop();
    }

    public void SendPacket(byte[] payload)
    {
        if (m_socket == null || m_serverEndPoint == null || payload == null) return;

        try
        {
            int sent = SendRaw(m_socket, payload, m_serverEndPoint);                 // TODO 4
            if (sent < 0) WarnTodo(4, "SendRaw() is not implemented yet");
        }
        catch (SocketException e) { Log("[CLIENT] Send failed: " + e.SocketErrorCode); }
        catch (ObjectDisposedException) { }
    }

    public void SendString(string text)
    {
        SendPacket(Encoding.UTF8.GetBytes(text));
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[MaxPacketSize];

        while (m_running)
        {
            // Blank endpoint: ReceiveFrom fills it with the sender. Here it is always the server,
            // so you do not really need it, but the API asks for it anyway (slide 10).
            EndPoint from = new IPEndPoint(IPAddress.Any, 0);

            int received;
            try { received = ReceiveRaw(m_socket, buffer, ref from); }                // TODO 5
            catch (SocketException e)
            {
                // Windows reports "nobody is listening there" as an error on receive, which is the
                // closest thing UDP has to "the server is down".
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Log("[CLIENT] No server on " + m_serverEndPoint + " (slide 34)");
                    return;
                }
                if (m_running) Log("[CLIENT] Receive failed: " + e.SocketErrorCode);
                return;
            }
            catch (ObjectDisposedException) { return; }

            if (received < 0) { WarnTodo(5, "ReceiveRaw() is not implemented yet"); return; }
            if (received == 0) continue;

            byte[] payload = new byte[received];
            Array.Copy(buffer, payload, received);

            m_inbox.Enqueue(payload);
        }
    }

    void OnPacketReceived(byte[] data)
    {
        string dataString = Encoding.UTF8.GetString(data);
        //split string to check for commands
        string[] splitString = dataString.Split('_');

        //check if what has been receieved is a command from server
        //if so --> dont show on screen
        bool isCommand = false;

        if(splitString.Length > 0)
        {
            if (splitString[0] == "THECUBE")
            {
                if (float.TryParse(splitString[1], out float value))
                {
                    networkManager.cubeRot = value;
                }

                isCommand = true;
            }
        }

        if(Encoding.UTF8.GetString(data) == "STARTGAME")
        {
            //load game scene
            networkManager.LoadGameScene();
        } 

        if (!isCommand) Log(Encoding.UTF8.GetString(data));
    }

    void StartThread(ThreadStart work)
    {
        Thread t = new Thread(work);
        t.IsBackground = true;
        lock (m_threads) m_threads.Add(t);
        t.Start();
    }

    public void Log(string message)
    {
        Debug.Log(message);
        if (networkManager != null)
        {
            networkManager.Log(message);
        }
    }

    void WarnTodo(int number, string detail)
    {
        lock (m_warned) { if (!m_warned.Add(number)) return; }
        Log("[TODO " + number + "] not done yet: " + detail);
    }

    // =============================================================================================
    //  SOLUTION - the TODOs of the lab session, filled in
    // =============================================================================================

    // --------------------------------------------------------------------------------- TODO 1 ---
    //  Goal: create the client socket and return it.
    //  Shorter than you expect: no Connect(), and no Bind() either, because the first SendTo()
    //  makes the system choose a local port for you (slide 6).
    //  Careful with the socket type and the protocol: datagrams, not streams.
    Socket StartClient()
    {
        return new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    // --------------------------------------------------------------------------------- TODO 2 ---
    //  Goal: build and return the address of the server, using 'serverIp' and 'port'.
    //  This is the envelope you will write on every message you send (slide 10).
    //  Look it up: IPEndPoint and IPAddress in the docs. IPAddress.Parse turns "127.0.0.1" into an
    //  IPAddress. The return type is EndPoint, and IPEndPoint is an EndPoint.
    //  Console when it works:  [CLIENT] Ready, server is 127.0.0.1:9050
    EndPoint ServerEndPoint()
    {
        return new IPEndPoint(IPAddress.Parse(serverIp), port);
    }

    // --------------------------------------------------------------------------------- TODO 3 ---
    //  Goal: introduce yourself by sending 'userName' to the server.
    //  In TCP the connection was made by Connect(). Here there is no connection: this first
    //  message is the only thing that tells the server that you exist (slide 9).
    //  Use the helper that is already written: SendString(text).
    //  Console when it works: the server prints  [SERVER] Received: Player
    void OnStarted()
    {
        SendString(userName);   // no Connect: this first message IS the handshake
    }

    // --------------------------------------------------------------------------------- TODO 4 ---
    //  Goal: send 'data' through 'socket' to the address 'to', and return how many bytes went out.
    //  Look it up: Socket.SendTo in the docs.
    int SendRaw(Socket socket, byte[] data, EndPoint to)
    {
        return socket.SendTo(data, to);
    }

    // --------------------------------------------------------------------------------- TODO 5 ---
    //  Goal: receive one datagram into 'buffer' and return its size.
    //  Look it up: Socket.ReceiveFrom in the docs. 'from' is passed with 'ref' because the method
    //  fills it with the address of the sender.
    //  Console when it works:  [CLIENT] Received: MyServer
    int ReceiveRaw(Socket socket, byte[] buffer, ref EndPoint from)
    {
        return socket.ReceiveFrom(buffer, ref from);
    }
}
