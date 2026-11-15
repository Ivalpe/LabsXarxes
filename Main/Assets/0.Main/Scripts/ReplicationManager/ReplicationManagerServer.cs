using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MainSerializer;

// Actions for an object in the network
public enum ReplicationAction : byte
{
    Create = 1,
    Update = 2,
    Destroy = 3
}

public class ReplicationManagerServer
{
    // List of object IDs
    readonly LinkingContext m_linking;

    // Changes to send now
    readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();

    // Lists to repeat Create and Destroy for 1 second
    Dictionary<int, ReplicationAction> m_repeatAction = new Dictionary<int, ReplicationAction>();
    Dictionary<int, float> m_repeatUntil = new Dictionary<int, float>();

    // Packet number to ignore old packets
    int m_sequence;

    public ReplicationManagerServer(LinkingContext linking)
    {
        m_linking = linking;
    }

    // Add Create and repeat it for 1 second
    public void MarkCreate(int id)
    {
        m_pending[id] = ReplicationAction.Create;
        m_repeatAction[id] = ReplicationAction.Create;
        m_repeatUntil[id] = Time.time + 1f;
    }

    // Add Destroy and repeat it for 1 second
    public void MarkDestroy(int id)
    {
        m_pending[id] = ReplicationAction.Destroy;
        m_repeatAction[id] = ReplicationAction.Destroy;
        m_repeatUntil[id] = Time.time + 1f;
    }

    // Add Update if there is no Create or Destroy
    public void MarkUpdate(int id)
    {
        if (!m_pending.ContainsKey(id))
            m_pending[id] = ReplicationAction.Update;
    }

    // Make the packet with all changes
    public byte[] WritePacket()
    {
        // Check objects in the 1 second repeat time
        List<int> repeatIds = new List<int>(m_repeatAction.Keys);

        foreach (int id in repeatIds)
        {
            if (Time.time > m_repeatUntil[id]) // 1 second passed, stop repeat
            {
                m_repeatUntil.Remove(id);
                m_repeatAction.Remove(id);
            }
            else
            {
                m_pending[id] = m_repeatAction[id]; // Still in 1 second, send again
            }
        }

        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        // Packet header: Type + packet number + number of objects
        writer.Write((byte)MainSerializer.MsgType.Replication);
        writer.Write(m_sequence++);
        writer.Write(m_pending.Count);

        // Put each changed object in the packet
        foreach (KeyValuePair<int, ReplicationAction> entry in m_pending)
        {
            writer.Write(entry.Key);         // Object ID
            writer.Write((byte)entry.Value); // Action: Create, Update or Destroy

            if (entry.Value == ReplicationAction.Destroy) continue; // Destroy needs no more data

            GameObject go = m_linking.Find(entry.Key); // Find object by ID
            if (go == null) continue;

            // Write prefab type and object data
            NetObject obj = go.GetComponent<NetObject>();
            writer.Write(obj.type);
            obj.Write(writer);
        }

        // Clear list for the next turn
        m_pending.Clear();
        return stream.ToArray();
    }
}
