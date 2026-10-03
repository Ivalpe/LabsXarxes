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
    enum ConnectionType
    {
        UDP,
        TCP
    }

    public int port = 9050;
    public string serverName = "MyServer";
    public bool autoStart = true;

    const int MaxPacketSize = 64 * 1024;

    struct PacketUDP { public byte[] data; public EndPoint from; }
    struct PacketTCP { public byte[] data; public Socket from; }
    ConnectionType connectionType;
    Socket socket;
    public Dictionary<Socket, string> usersConnected = new Dictionary<Socket, string>();

    readonly List<Socket> m_clients = new List<Socket>(); //TCP Clients
    readonly HashSet<EndPoint> m_knownClients = new HashSet<EndPoint>(); //UDP Clients

    readonly List<Thread> m_threads = new List<Thread>();
    readonly ConcurrentQueue<PacketUDP> m_inboxUDP = new ConcurrentQueue<PacketUDP>();
    readonly ConcurrentQueue<PacketTCP> m_inboxTCP = new ConcurrentQueue<PacketTCP>();
    readonly List<string> m_log = new List<string>();
    readonly HashSet<int> m_warned = new HashSet<int>();
    volatile bool m_running;
    bool host = false;

    Connection(ConnectionType connectionType)
    {
        this.connectionType = connectionType;
    }

    void Start()
    {
        Application.runInBackground = true;
        ParseCommandLine();
        if (autoStart) StartNetwork();
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
        ReceiveLoop(client);
        lock (m_clients) m_clients.Remove(client);
        Log("[SERVER] " + usersConnected[client] + " disconnected");

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
    void ReceiveLoop(Socket socket)
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
    void OnPacketReceived(byte[] data, Socket from)
    {
        string text = Encoding.UTF8.GetString(data);

        if (connectionType == ConnectionType.TCP)
        {
            foreach (var user in usersConnected)
            {
                if (user.Value == "" && from == user.Key)
                {
                    usersConnected[user.Key] = text;
                }

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
        lock (m_log) { m_log.Add(message); if (m_log.Count > 100) m_log.RemoveAt(0); }
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
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, Screen.width - 20, Screen.height - 20));
        GUILayout.Label("TCP SERVER   port " + port + "   name " + serverName);

        if (!m_running) { if (GUILayout.Button("Start server", GUILayout.Width(110))) StartNetwork(); }
        else if (GUILayout.Button("Stop", GUILayout.Width(110))) Disconnect();

        lock (m_log)
            for (int i = Mathf.Max(0, m_log.Count - 18); i < m_log.Count; i++)
                GUILayout.Label(m_log[i]);

        GUILayout.EndArea();
    }
}
