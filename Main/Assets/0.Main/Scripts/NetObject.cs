using System.IO;
using UnityEngine;


public class NetObject : MonoBehaviour
{
    public int id;          // network ID, from the linking context
    public byte type;       // which prefab, the same table on both sides

    public virtual void Write(BinaryWriter w)
    {
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.position.z);
        w.Write(transform.eulerAngles.y);
    }

    public virtual void Read(BinaryReader r)
    {
        transform.position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        transform.rotation = Quaternion.Euler(0f, r.ReadSingle(), 0f);
    }
}

