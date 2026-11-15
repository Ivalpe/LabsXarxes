using UnityEngine;
using System.IO;

public class NetObject : MonoBehaviour
{

    public int id;
    public byte type;

    // Server writes object position and rotation
    public virtual void Write(BinaryWriter w)
    {
        w.Write(transform.position.x);
        w.Write(transform.position.y);
        w.Write(transform.position.z);
        w.Write(transform.eulerAngles.y);
    }

    // Client reads position and rotation from packet and moves object
    public virtual void Read(BinaryReader r)
    {
        transform.position = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        transform.rotation = Quaternion.Euler(0f, r.ReadSingle(), 0f);
    }
}
