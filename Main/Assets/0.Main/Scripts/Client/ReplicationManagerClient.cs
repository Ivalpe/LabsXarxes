using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class ReplicationManagerClient : MonoBehaviour
{
    [SerializeField] List<GameObject> m_prefabs = new List<GameObject>();   // index = type
    LinkingContext m_linking = new LinkingContext();
    int m_lastSequence = -1;

    
    public void Init(LinkingContext linking, List<GameObject> prefabs = null)
    {
        m_linking = linking;
        if (prefabs != null) m_prefabs = prefabs;
    }

    public void ReadPacket(BinaryReader reader)             
    {
        int sequence = reader.ReadInt32();
        if (sequence <= m_lastSequence) return;              // older than what we have
        m_lastSequence = sequence;

        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            ReplicationAction action = (ReplicationAction)reader.ReadByte();
            GameObject go = m_linking.Find(id);              // null if this client does not know it

            if (action == ReplicationAction.Destroy)
            {
                if (go != null) { Destroy(go); m_linking.Unregister(id); }
                continue;                                    // unknown ID, already gone
            }

            byte type = reader.ReadByte();
            if (go == null)                                  // Create, or an Update whose Create was lost
            {
                go = Instantiate(m_prefabs[type]);
                go.GetComponent<NetObject>().id = id;
                m_linking.Register(go, id);
            }
            go.GetComponent<NetObject>().Read(reader);
        }
    }
}

