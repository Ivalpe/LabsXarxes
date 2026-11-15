using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MainSerializer;

public class ReplicationManagerServer
{
    readonly LinkingContext m_linking;
    readonly Dictionary<int, ReplicationAction> m_pending = new Dictionary<int, ReplicationAction>();
    readonly Dictionary<int, (ReplicationAction action, float until)> m_repeat =
        new Dictionary<int, (ReplicationAction, float)>();
    int m_sequence;
    public Func<float> clock = () => Time.time;

    public ReplicationManagerServer (LinkingContext linking) { m_linking = linking; }

    
    public void MarkCreate(int id)
    {
        m_pending[id] = ReplicationAction.Create;
        m_repeat[id] = (ReplicationAction.Create, clock() + 1f);       // repeated for 1 s
    }

    public void MarkDestroy(int id)
    {
        m_pending[id] = ReplicationAction.Destroy;
        m_repeat[id] = (ReplicationAction.Destroy, clock() + 1f);
    }

    public void MarkUpdate(int id)
    {
        if (!m_pending.ContainsKey(id))                                 // a Create or Destroy wins
            m_pending[id] = ReplicationAction.Update;
    }

    public byte[] WritePacket()                                         // once per tick
    {
        foreach (int id in m_repeat.Keys.ToList())                      // given, slide 15
        {
            if (clock() > m_repeat[id].until) m_repeat.Remove(id);
            else m_pending[id] = m_repeat[id].action;
        }

        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        
        writer.Write((byte)MainSerializer.MsgType.Replication);                   
        writer.Write(m_sequence++);
        writer.Write(m_pending.Count);

        foreach (KeyValuePair<int, ReplicationAction> entry in m_pending)
        {
            
            writer.Write(entry.Key);                                // 2. identify the object
            writer.Write((byte)entry.Value);
            if (entry.Value == ReplicationAction.Destroy) continue; // nothing else to say
            NetObject obj = m_linking.Find(entry.Key).GetComponent<NetObject>();
            writer.Write(obj.type);                                 // 3. its class, which prefab
            obj.Write(writer);                                      // 4. its fields
        }
        m_pending.Clear();
        return stream.ToArray();
    }
}

public enum ReplicationAction : byte { Create = 1, Update = 2, Destroy = 3 }
