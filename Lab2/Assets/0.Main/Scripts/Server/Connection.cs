using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using static MainSerializer;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class Connection : MonoBehaviour
{
    public enum ConnectionType
    {
        UDP,
        TCP
    }

    ConnectionManager connectionManager;
    BaseSerializer serializer;

    public int port;
    public string serverName = "MyServer";
    public bool autoStart = true;

    const int MaxPacketSize = 64 * 1024;

    struct PacketUDP { public byte[] data; public EndPoint from; }
    struct PacketTCP { public byte[] data; public Socket from; }
    ConnectionType connectionType;
    Socket socket;
    public Dictionary<Socket, PlayerInfo> usersConnected = new Dictionary<Socket, PlayerInfo>();

    //handle pings (UDP)
    readonly Dictionary<EndPoint, float> udpPings = new Dictionary<EndPoint, float>();
    float maxTime = 5f;

    readonly List<Socket> m_clients = new List<Socket>(); //TCP Clients
    readonly HashSet<EndPoint> m_knownClients = new HashSet<EndPoint>(); //UDP Clients

    readonly List<Thread> m_threads = new List<Thread>();
    readonly ConcurrentQueue<PacketUDP> m_inboxUDP = new ConcurrentQueue<PacketUDP>();
    readonly ConcurrentQueue<PacketTCP> m_inboxTCP = new ConcurrentQueue<PacketTCP>();
    readonly HashSet<int> m_warned = new HashSet<int>();
    volatile bool m_running;

    //client host handling
    bool host = false;
    Socket hostSocket;

    float velocityRotation = 0.5f;


    //State puzzle
    bool isPuzzleFinished = false;

    //player id handling
    int nextClientId = 1;

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
    int nextColorIndex = 0;

    //spawn position 
    Vector3 playerSpawnPos = new Vector3(0f, 0f, 0f);
    float playerSpawnXVar = 3f;


    public bool IsRunning { get { return m_running; } }
    public int ClientCount { get { lock (m_knownClients) return m_knownClients.Count; } }
    public void SetPort(int p)
    {
        port = p;
    }

    public void SetType(ConnectionType ct)
    {
        connectionType = ct;
    }

    public HashSet<EndPoint> GetUDPClients()
    {
        return m_knownClients;
    }

    void Update()
    {
        if (connectionType == ConnectionType.TCP)
        {
            PacketTCP packet;
            while (m_inboxTCP.TryDequeue(out packet))
                OnPacketReceivedTCP(packet.data, packet.from);
        }
        else
        {
            PacketUDP packet;
            while (m_inboxUDP.TryDequeue(out packet))
                OnPacketReceivedUDP(packet.data, packet.from);
        }

        // Check for inactive clients
        if (connectionType == ConnectionType.UDP)
        {
            List<EndPoint> disconnectedClients = new List<EndPoint>();
            List<EndPoint> activeClients = new List<EndPoint>(udpPings.Keys);

            foreach (var user in activeClients)
            {
                udpPings[user] += Time.deltaTime;

                //if timer hits 5s --> user is disconnected
                if (udpPings[user] >= maxTime)
                {
                    disconnectedClients.Add(user);
                }
            }

            foreach (var user in disconnectedClients)
            {
                //remove disconnected clients from lists
                udpPings.Remove(user);
                lock (m_knownClients) m_knownClients.Remove(user);
                Log("[SERVER] Client disconnected due to inactivity: " + user);
            }
        }

    }

    void Start()
    {
        Application.runInBackground = true;
        //ParseCommandLine();
        //if (autoStart) StartNetwork();

        connectionManager = GetComponent<ConnectionManager>();
        serializer = connectionManager.serializer;
    }

    public void StartNetwork()
    {
        if (m_running) return;
        m_running = true;
        StartThread(ServerThread);
    }

    public void Disconnect()
    {
        if (connectionType == ConnectionType.UDP)
        {
            if (!m_running) return;
            m_running = false;

            // Closing the socket unblocks the thread waiting in ReceiveFrom() (slide 14).
            // No Shutdown() here: UDP has no connection to shut down.
            if (socket != null) { try { socket.Close(); } catch { } socket = null; }


           // PlayerInfo //He comentado esto

            lock (m_knownClients) m_knownClients.Clear();
            udpPings.Clear();

            Thread[] threads;
            lock (m_threads) { threads = m_threads.ToArray(); m_threads.Clear(); }
            foreach (Thread t in threads) if (t != Thread.CurrentThread) t.Join(500);

            Log("[SERVER] Stopped");
        }
        else
        {
            if (!m_running) return;
            m_running = false;

            Socket[] clients;
            lock (m_clients) { clients = m_clients.ToArray(); m_clients.Clear(); }
            foreach (Socket c in clients) CloseSocket(c);

            CloseSocket(socket); socket = null;

            Thread[] threads;
            lock (m_threads) { threads = m_threads.ToArray(); m_threads.Clear(); }
            foreach (Thread t in threads) if (t != Thread.CurrentThread) t.Join(500);

            Log("[SERVER] Stopped");

            usersConnected.Clear();
        }
    }

    void ServerThread()
    {
        if (connectionType == ConnectionType.TCP)
        {
            try { socket = StartServer(); }
            catch (SocketException e)
            {
                Log("[SERVER] Could not start: " + e.SocketErrorCode +
                    (e.SocketErrorCode == SocketError.AddressAlreadyInUse
                        ? " (port " + port + " is still used by another instance, slide 34)" : ""));
                return;
            }

            if (socket == null) { WarnTodo(1, "StartServer() returned null"); return; }
            Log("[SERVER] Listening on " + port);

            while (m_running)
            {
                Socket client;
                try { client = AcceptClient(); }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }

                if (client == null) { WarnTodo(2, "AcceptClient() returned null"); return; }

                lock (m_clients) m_clients.Add(client);
                Log("[SERVER] Client connected: " + client.RemoteEndPoint);

                //get assigned color
                byte colorIndex = (byte)(nextColorIndex % playerColors.Count);
                nextColorIndex++;

                PlayerInfo clientPlayer = new PlayerInfo(nextClientId, "", playerSpawnPos, Vector3.zero, Vector3.one,0.0f, colorIndex, playerColors[colorIndex]);
                playerSpawnPos.x += playerSpawnXVar;
                nextClientId++;

                usersConnected.Add(client, clientPlayer);

                Socket captured = client;
                StartThread(delegate { ClientThread(captured); });
            }
        }
        else
        {
            try { socket = StartServer(); }
            catch (SocketException e)
            {
                Log("[SERVER] Could not start: " + e.SocketErrorCode +
                    (e.SocketErrorCode == SocketError.AddressAlreadyInUse
                        ? " (port " + port + " is still used by another instance, slide 34)" : ""));
                return;
            }

            if (socket == null) { WarnTodo(1, "StartServer() returned null"); return; }
            Log("[SERVER] Listening on " + port);

            ReceiveLoopUDP();
        }
    }

    void ReceiveLoopTCP(Socket socket)
    {
        byte[] header = new byte[4];
        while (m_running)
        {
            if (!ReadExactly(socket, header, 4)) return;

            int size = BitConverter.ToInt32(header, 0);
            if (size <= 0 || size > MaxPacketSize) { Log("[SERVER] Invalid packet size: " + size); return; }

            byte[] payload = new byte[size];
            if (!ReadExactly(socket, payload, size)) return;

            m_inboxTCP.Enqueue(new PacketTCP { data = payload, from = socket });
        }

    }

    bool ReadExactly(Socket socket, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read;
            try { read = ReceiveRawTCP(socket, buffer, total, count - total); }         // TODO 4
            catch (SocketException e) { Log("[SERVER] " + usersConnected[socket] + "Lost Connection: " + e.SocketErrorCode); return false; }
            catch (ObjectDisposedException) { return false; }

            if (read < 0) { WarnTodo(4, "ReceiveRaw() is not implemented yet"); return false; }
            if (read == 0) return false;   // 0 bytes = the client closed the connection (slide 8)
            total += read;
        }
        return true;
    }
    int ReceiveRawTCP(Socket socket, byte[] buffer, int offset, int count)
    {
        return socket.Receive(buffer, offset, count, SocketFlags.None);
    }
    int ReceiveRawUDP(Socket socket, byte[] buffer, ref EndPoint from)
    {
        return socket.ReceiveFrom(buffer, ref from);   // 'from' comes back filled with the sender
    }
    void ReceiveLoopUDP()
    {
        byte[] buffer = new byte[MaxPacketSize];

        while (m_running)
        {
            // A blank endpoint: ReceiveFrom OVERWRITES it with the address of whoever sent the
            // datagram. That is how a UDP server learns where to reply (slides 9 and 10).
            EndPoint from = new IPEndPoint(IPAddress.Any, 0);

            int received;
            try { received = ReceiveRawUDP(socket, buffer, ref from); }                // TODO 3
            catch (SocketException e)
            {
                // Windows reports "a previous SendTo reached a closed port" as an error on receive.
                // Not our problem: keep serving.
                if (e.SocketErrorCode == SocketError.ConnectionReset) continue;
                if (m_running) Log("[SERVER] Receive failed: " + e.SocketErrorCode);
                return;
            }
            catch (ObjectDisposedException) { return; }

            if (received < 0) { WarnTodo(3, "ReceiveRaw() is not implemented yet"); return; }
            if (received == 0) continue;

            byte[] payload = new byte[received];
            Array.Copy(buffer, payload, received);

            m_inboxUDP.Enqueue(new PacketUDP { data = payload, from = from });
        }
    }
    void ClientThread(Socket client)
    {
        ReceiveLoopTCP(client);

        bool noHost = false;
        string clientName = "";
        List<Socket> remainingClients = new List<Socket>();
        List<string> updatedClientList = new List<string>();

        lock (usersConnected)
        {
            //check if user disconnected is host
            if (client == hostSocket) noHost = true;

            // remove disconnected client from list
            if (usersConnected.TryGetValue(client, out PlayerInfo clientPlayer))
            {
                if(clientPlayer != null)
                {
                    clientName = clientPlayer.playerName;
                }
                connectionManager.playerInfoDic.Remove(clientPlayer.id);
                usersConnected.Remove(client);
            }

            if (noHost)
            {
                hostSocket = null;
                host = false;

                //if host is gone --> prepare list of clients to disconnect
                remainingClients = new List<Socket>(usersConnected.Keys);
                usersConnected.Clear();
                connectionManager.playerInfoDic.Clear();
            }
            else
            {
                //create new user list (after removing disconnected user)
                foreach (var player in usersConnected.Values)
                {
                    if (!string.IsNullOrEmpty(player.playerName)) updatedClientList.Add(player.playerName);
                }

                //get all remaining sockets
                remainingClients = new List<Socket>(usersConnected.Keys);

                //if no users --> no host user
                if (usersConnected.Count == 0)
                {
                    host = false;
                    hostSocket = null;
                }
            }
        }

        lock (m_clients)
        {
            m_clients.Remove(client);
            if (noHost) m_clients.Clear();
        }

        Log("[SERVER] " + clientName + " disconnected");

        if (noHost)
        {
            //kick everyone if host is gone
            foreach (var otherClient in remainingClients) CloseSocket(otherClient);
        }
        else
        {
            foreach (var user in usersConnected)
            {
                //send user list
                SendPacketTCP(serializer.SerializeUserList(updatedClientList), user.Key);
            }
        }

        CloseSocket(client);
    }
    void CloseSocket(Socket socket)
    {
        if (socket == null) return;
        try { socket.Shutdown(SocketShutdown.Both); } catch { }
        try { socket.Close(); } catch { }
    }
    void StartThread(ThreadStart work)
    {
        Thread t = new Thread(work);
        t.IsBackground = true;
        lock (m_threads) m_threads.Add(t);
        t.Start();
    }
    Socket StartServer()
    {
        if (connectionType == ConnectionType.TCP)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(10);
            return socket;
        }
        else
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return socket;
        }
    }
    Socket AcceptClient()
    {
        return socket.Accept();
    }
    void OnPacketReceivedTCP(byte[] data, Socket from)
    {
        serializer.Deserialize(data, from);
    }

    public void TCPSendChatMessage(Socket from, string text)
    {
        string chatText = "[" + usersConnected[from].playerName + "]: " + text;

        foreach (var user in usersConnected)
        {
            SendPacketTCP(serializer.SerializeText(chatText), user.Key);
        }

        Log(chatText);
    }

    public void TCPNewUser(Socket from, string name)
    {
        if (usersConnected.ContainsKey(from) && usersConnected[from].playerName == "")
        {
            usersConnected[from].playerName = name;

            //send id to client and assign to id dictionary
            int assignedId = usersConnected[from].id;
            SendPacketTCP(serializer.SerializePlayerId(assignedId), from);
            connectionManager.playerInfoDic[assignedId] = usersConnected[from];

            List<string> users = new List<string>(usersConnected.Count);
            foreach (var player in usersConnected.Values) users.Add(player.playerName);

            if (!isPuzzleFinished)
            {
                foreach (var user in usersConnected)
                {
                    Debug.Log("user: " + user.Value.playerName);


                    //send user list
                    SendPacketTCP(serializer.SerializeUserList(users), user.Key);
                }
            }
            else //When the puzzle is already finished, late players bypass the lobby and load the game scene immediately
            {
                SendPacketTCP(serializer.SerializeUserList(users), from);
                SendPacketTCP(serializer.SerializeTypes(false, CommandType.StartGame), from);

            }
        }

        if (!host)
        {
            //send host command to make user host
            SendPacketTCP(serializer.SerializeTypes(false, CommandType.Host), from);
            host = true;
            hostSocket = from;
        }
    }

    void OnPacketReceivedUDP(byte[] data, EndPoint from)
    {
        //if communication is received --> reset ping timer (and add it to pings list, if first packet received)
        udpPings[from] = 0f;

        serializer.Deserialize(data, null, from);
    }

    public void RegisterUDPClient(EndPoint from)
    {
        bool isNew;
        lock (m_knownClients)
        {
            isNew = m_knownClients.Add(from);
        }

        if (isNew)
        {
            Log("[SERVER] New UDP client: " + from);
        }

        byte[] response = serializer.SerializeText(serverName, CommandType.ConnectedUDP);
        SendPacketUDP(response, from);
    }

    public void UDPUpdateCube(float rot)
    {
        float rotation = rot + velocityRotation;
        foreach(var user in m_knownClients)
        {
            SendPacketUDP(serializer.SerializeCubeRot(rotation), user);
        }
    }

    public void UDPSendToAllClients(byte[] packet)
    {
        foreach (var user in m_knownClients)
        {
            SendPacketUDP(packet, user);
        }
    }

    public void UDPStartGame()
    {
        //Puzzle is finish
        isPuzzleFinished = true;
        connectionManager.SetUpGameScene();
        connectionManager.Log("[SERVER] Started playing.");

        connectionManager.ResetDoorState();

        //get player list (for spawning prefabs)
        List<PlayerInfo> list = new List<PlayerInfo>(connectionManager.playerInfoDic.Values);

        foreach (var user in m_knownClients)
        {
            SendPacketUDP(serializer.SerializeWorldPos(list, true), user);
        }
    }
    public void SendStringTCP(string text, Socket to)
    {
        SendPacketTCP(Encoding.UTF8.GetBytes(text), to);
    }
    public void SendStringUDP(string text, EndPoint to)
    {
        SendPacketUDP(Encoding.UTF8.GetBytes(text), to);
    }
    public void SendPacketTCP(byte[] payload, Socket to)
    {
        if (to == null || payload == null) return;

        byte[] framed = new byte[4 + payload.Length];
        BitConverter.GetBytes(payload.Length).CopyTo(framed, 0);
        payload.CopyTo(framed, 4);

        try
        {
            int sent = SendRawTCP(to, framed);                                          // TODO 3
            if (sent < 0) WarnTodo(3, "SendRaw() is not implemented yet");
        }
        catch (SocketException e) { Log("[SERVER] Send failed: " + e.SocketErrorCode); }
        catch (ObjectDisposedException) { }
    }
    public void SendPacketUDP(byte[] payload, EndPoint to)
    {
        if (socket == null || to == null || payload == null) return;

        try
        {
            int sent = SendRawUDP(socket, payload, to);                               // TODO 2
            if (sent < 0) WarnTodo(2, "SendRaw() is not implemented yet");
        }
        catch (SocketException e) { Log("[SERVER] Send failed: " + e.SocketErrorCode); }
        catch (ObjectDisposedException) { }
    }
    public void Log(string message)
    {
        Debug.Log(message);
        if (connectionManager != null) connectionManager.Log(message);
    }
    void WarnTodo(int number, string detail)
    {
        lock (m_warned) { if (!m_warned.Add(number)) return; }
        Log("[TODO " + number + "] not done yet: " + detail);
    }
    int SendRawTCP(Socket socket, byte[] data)
    {
        return socket.Send(data);
    }
    int SendRawUDP(Socket socket, byte[] data, EndPoint to)
    {
        return socket.SendTo(data, to);   // the destination travels with every single message
    }
}
