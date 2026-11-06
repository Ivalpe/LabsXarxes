using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetSimulator : MonoBehaviour
{
    public float latency = 0.1f, jitter = 0.03f, dropChance = 0.02f;   // seconds, seconds, 0 to 1

    public Socket m_socket { get; set; }
    readonly List<(float at, byte[] data, EndPoint to)> m_queue = new List<(float, byte[], EndPoint)>();

    public NetSimulator(Socket socket) { m_socket = socket; }

    // Packets waiting to be sent. For the debug overlay.
    public int Pending { get { return m_queue.Count; } }

    public void Send(byte[] data, EndPoint to)
    {
        if (Random.value < dropChance) return; //lost packet
        float delay = latency + Random.Range(-jitter, jitter); //reorder chance
        m_queue.Add((Time.time + delay, data, to));
    }

    public void Update()
    {
        if (m_socket == null) return;

        for (int i = 0; i < m_queue.Count;)
        {
            if (Time.time >= m_queue[i].at)
            {
                m_socket.SendTo(m_queue[i].data, m_queue[i].to);
                m_queue.RemoveAt(i);
            }
            else i++;
        }
    }
}
