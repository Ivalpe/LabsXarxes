using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class NetSimulator
{
    public static float latency = 0.1f;
    public static float jitter = 0.03f;
    public static float dropChance = 0.02f;   // seconds, seconds, 0 to 1
    public static bool enabled = true;

    public Socket m_socket { get; set; }
    readonly List<(float at, byte[] data, EndPoint to)> m_queue = new List<(float, byte[], EndPoint)>();

    public NetSimulator(Socket socket) { m_socket = socket; }

    // Packets waiting to be sent. For the debug overlay.
    public int Pending { get { return m_queue.Count; } }

    public static void SetBadConditions()
    {
        latency = 0.45f;
        jitter = 0.15f;
        dropChance = 0.20f;
        Debug.Log("[NetSim] F2 -> BAD Conditions (150ms lag, 5% drop)");
    }

    public static void SetBaseConditions()
    {
        latency = 0.15f;     
        jitter = 0.05f;      
        dropChance = 0.05f;  
        Debug.Log("[NetSim] F2 -> BASE Conditions (150ms lag, 5% drop)");
    }

    public static void ToggleConditions()
    {
        enabled = !enabled;
        Debug.Log("[NetSim] F1 -> " + (enabled ? "ENABLED" : "DISABLED"));
    }

    public void Send(byte[] data, EndPoint to)
    {
        if (m_socket == null) return;

        if (!enabled)
        {
            m_socket.SendTo(data, to);
            return;
        }

        if (Random.value < dropChance) return; //lost packet
        float delay = Mathf.Max(0f, latency + Random.Range(-jitter, jitter)); //reorder chance
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
