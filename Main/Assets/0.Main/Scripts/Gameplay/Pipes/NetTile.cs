using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum netColor : byte
{
    red = 1,
    blue = 2,
    green = 3,
    orange = 4,
    magenta = 5,
    cyan = 6,

}

public class NetTile : NetObject
{
    public int boardX;
    public int boardY;
    public int rotIndex;
    public byte color;

    
    
    public override void Write(BinaryWriter w) 
    {
        //base.Write(w); //--> writes position, rotation.y
        //w.Write(transform.eulerAngles.z); we don't send the rotation, we send the board index and rotation index
        //w.Write(base.type); //--> do I need this?
        w.Write(boardX);
        w.Write(boardY);
        w.Write(rotIndex);
        w.Write(color);

    }
    public override void Read(BinaryReader r) 
    {
        //base.Read(r); //--> reads position, rotation.y
        //transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y, r.ReadSingle()); 
   
        boardX = r.ReadInt32();
        boardY = r.ReadInt32();
        rotIndex = r.ReadInt32();
        color = r.ReadByte();

        PipeControl boardScript = GameObject.FindAnyObjectByType<PipeControl>();
        Pipe[,] pMap = boardScript.pipeMap;

        if (pMap[boardX, boardY].tileGO.TryGetComponent<TubeTile>(out TubeTile tileScript))
        {
            tileScript.StartRotation(pMap[boardX, boardY]); //updates rotindex at the end
        }
    }
}
