using System.Drawing;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

public class NetBoard : NetObject
{
    public override void Write(BinaryWriter w)
    {
        base.Write(w); //--> writes position, rotation.y
        w.Write(transform.eulerAngles.x);
        w.Write(transform.eulerAngles.z); 
        


    }
    public override void Read(BinaryReader r)
    {
        base.Read(r); //--> reads position, rotation.y
        
        float rotX = r.ReadSingle();
        float rotZ = r.ReadSingle();
        transform.rotation = Quaternion.Euler(rotX, transform.rotation.eulerAngles.y, rotZ);

    }
}
