using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static MainSerializer;

public class ReplicationManagerClient : MonoBehaviour
{
    // List of prefabs (must be in the same order as the server)
    [SerializeField] 
    List<GameObject> m_prefabs = new List<GameObject>();

    // List of object IDs on the client
    LinkingContext m_linking = new LinkingContext();

    // Last packet number received
    int m_lastSequence = -1;

    // Set the linking context and prefabs
    public void Init(LinkingContext linking, List<GameObject> prefabs = null)
    {
        m_linking = linking;
        if (prefabs != null) m_prefabs = prefabs;
    }

    public void ReadPacket(BinaryReader reader)
    {
        // Read packet number and ignore old packets
        int sequence = reader.ReadInt32();
        if (sequence <= m_lastSequence) return;
        m_lastSequence = sequence;

        // Read how many objects are in the packet
        int count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            ReplicationAction action = (ReplicationAction)reader.ReadByte();

            // Find the object on the client
            GameObject go = m_linking.Find(id);

            // If Destroy, delete object and remove ID
            if (action == ReplicationAction.Destroy)
            {
                if (go != null)
                {
                    Destroy(go);
                    m_linking.Unregister(id);
                }
                continue;
            }

            // Read prefab type
            byte type = reader.ReadByte();

            // If object is not on client yet, create it
            if (go == null)
            {
                go = Instantiate(m_prefabs[type]);
                go.GetComponent<NetObject>().id = id;
                m_linking.Register(go, id);
            }

            // Read object position and data
            go.GetComponent<NetObject>().Read(reader);
        }
    }
}
