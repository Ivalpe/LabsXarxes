using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class Connection : MonoBehaviour
{
    public enum ConnectionType
    {
        UDP,
        TCP
    }

    ConnectionManager connectionManager;

    public int port;
    public string serverName = "MyServer";
    public bool autoStart = true;

    const int MaxPacketSize = 64 * 1024;

    struct PacketUDP { public byte[] data; public EndPoint from; }
    struct PacketTCP { public byte[] data; public Socket from; }
    ConnectionType connectionType;
    Socket socket;
    public Dictionary<Socket, string> usersConnected = new Dictionary<Socket, string>();
    public Dictionary<Socket, string> lastConnection = new Dictionary<Socket, string>();

    readonly List<Socket> m_clients = new List<Socket>(); //TCP Clients
    readonly HashSet<EndPoint> m_knownClients = new HashSet<EndPoint>(); //UDP Clients

    readonly List<Thread> m_threads = new List<Thread>();
    readonly ConcurrentQueue<PacketUDP> m_inboxUDP = new ConcurrentQueue<PacketUDP>();
    readonly ConcurrentQueue<PacketTCP> m_inboxTCP = new ConcurrentQueue<PacketTCP>();
    readonly HashSet<int> m_warned = new HashSet<int>();
    volatile bool m_running;
    bool host = false;

    float velocityRotation = 0.5f;

    public List<float> timers = new List<float>();
    float maxTime = 5f;

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
            List<Socket> disconnectedClients = new List<Socket>();
            foreach (var user in usersConnected)
            {
                if (lastConnection.ContainsKey(user.Key))
                {
                    DateTime lastPingTime = DateTime.Parse(lastConnection[user.Key]);
                    TimeSpan timeSinceLastPing = DateTime.Now - lastPingTime;
                    if (timeSinceLastPing.TotalSeconds > maxTime)
                    {
                        disconnectedClients.Add(user.Key);
                    }
                }
            }
            foreach (var client in disconnectedClients)
            {
                Log("[SERVER] " + usersConnected[client] + " disconnected due to inactivity");
                usersConnected.Remove(client);
                lastConnection.Remove(client);
                CloseSocket(client);
            }
        }

    }

    void Start()
    {
        Application.runInBackground = true;
        //ParseCommandLine();
        //if (autoStart) StartNetwork();

        connectionManager = GetComponent<ConnectionManager>();
    }

    void ParseCommandLine()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-port" && i + 1 < args.Length) int.TryParse(args[++i], out port);
            else if (args[i] == "-name" && i + 1 < args.Length) serverName = args[++i];
            else if (args[i] == "-autostart") autoStart = true;
            else if (args[i] == "-noautostart") autoStart = false;
        }
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

            lock (m_knownClients) m_knownClients.Clear();

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

                usersConnected.Add(client, "");

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
        lock (m_clients) m_clients.Remove(client);
        Log("[SERVER] " + usersConnected[client] + " disconnected");

        foreach (var user in usersConnected)
        {
            SendStringTCP("LISTREMOVE_" + usersConnected[client], user.Key);
            Debug.Log("LISTREMOVE_" + usersConnected[client]);
        }

        //Close all users if host leaves
        if (client == usersConnected.First().Key)
        {
            foreach (var user in usersConnected)
            {
                CloseSocket(user.Key);
            }
            usersConnected.Clear();
        }

        usersConnected.Remove(client);

        if (usersConnected.Count == 0) host = false;


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
        string text = Encoding.UTF8.GetString(data);

        if (connectionType == ConnectionType.TCP)
        {
            if (usersConnected.ContainsKey(from) && usersConnected[from] == "")
            {
                usersConnected[from] = text;
                // Add the last time the user ping the server
                lastConnection.Add(from, DateTime.Now.ToString());

                String allUsers = string.Join("_", usersConnected.Values);

                foreach (var user in usersConnected)
                {
                    //Check if is the last user to join, if so send the list of users to all users
                    if (user.Key == usersConnected.Last().Key)
                    {
                        SendStringTCP("ALLLIST_" + allUsers, user.Key);
                        Debug.Log("ALLLIST_" + allUsers);
                    }
                    else
                    {
                        SendStringTCP("LISTADD_" + text, user.Key);
                        Debug.Log("LISTADD_" + text);
                    }
                }
            }

            foreach (var user in usersConnected)
            {
                if (!host)
                {
                    SendStringTCP("H", user.Key);
                    host = true;
                }

                SendStringTCP("[" + usersConnected[from] + "]: " + text, user.Key);
            }

            Log("[" + usersConnected[from] + "]: " + text);

        }
    }

    void OnPacketReceivedUDP(byte[] data, EndPoint from)
    {
        string text = Encoding.UTF8.GetString(data);
        Log("[SERVER] Received: " + text + " from " + from);

        if (text == "STARTGAME")
        {
            //Load a new scene for all clients
            foreach (var user in m_knownClients)
                SendStringUDP("STARTGAME", user);
        }

        // Send rotation to all clients
        string[] splitArray = text.Split(char.Parse("_"));
        if (splitArray[0] == "THECUBE")
        {
            float rotation = float.Parse(splitArray[1]) + velocityRotation; ;
            foreach (var user in m_knownClients)
                SendStringUDP("THECUBE_" + rotation, user);
        }

        // Register the ping from the client to update the last connection time
        if (splitArray[0] == "PING")
        {
            foreach (var user in usersConnected)
            {
                if (user.Key.RemoteEndPoint.ToString() == from.ToString())
                {
                    lastConnection[user.Key] = DateTime.Now.ToString();
                    break;
                }
            }
        }

        bool isNew;
        lock (m_knownClients) isNew = m_knownClients.Add(from);
        if (isNew) Log("[SERVER] New client: " + from);

        SendStringUDP(serverName, from);
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
