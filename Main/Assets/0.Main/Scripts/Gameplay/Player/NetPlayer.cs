using System.IO;
using UnityEngine;

public class NetPlayer : NetObject
{

    //public int health = 100;
    public override void Write(BinaryWriter w) { base.Write(w); }
    public override void Read(BinaryReader r) { base.Read(r); }
}
