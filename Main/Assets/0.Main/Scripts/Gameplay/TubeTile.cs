using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TubeTile : NetObject
{ 
    bool rotating = false;
    public float turnDuration = 0.5f;

    float elapsedTime = 0;
    float emissionTimer = 0f;

    public PipeControl pipeControl;

    public int xIndex = 0, yIndex = 0; // client & server fill them in

    public Color tileColor;
    public Material mat;
    int rotIndex = 0;
    void Start()
    {
        mat = GetComponent<Renderer>().material;
        mat.SetColor("_EmissionColor", Color.black);
        pipeControl = GetComponentInParent<PipeControl>();
    }

    //Emission to show what tile is being rotated 
    public void EnableEmission()
    {
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", tileColor * 0.5f);

        
    }

    public void  DisableEmission()
    {

        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        mat.SetColor("_EmissionColor", Color.black);
        
    }

    public void StartRotation(Pipe p, ReplicationManagerServer replServer = null)
    {
        if (!rotating)
        {
           
            if (p.rotIndex > 4) p.rotIndex -= 4;
            if (p.exit1 > 4) p.exit1 -= 4;
            if (p.exit2 > 4) p.exit2 -= 4;

            rotIndex = p.rotIndex;
            Quaternion targetRot = Quaternion.Euler(new Vector3(0,0, rotIndex * 90f));
            StartCoroutine(Rotate(p, targetRot, replServer));
        }
        
    }

    //Rotation for the tiles in the board
    IEnumerator Rotate(Pipe p, Quaternion targetRot, ReplicationManagerServer replServer = null)
    {
        
        while (elapsedTime <= turnDuration)
        {
            elapsedTime += Time.deltaTime;
            rotating = true;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Mathf.Min(elapsedTime, 1.0f));
            
            yield return null;
        }

        rotating = false;

        p.rotIndex++;
        p.exit1++;
        p.exit2++;

        elapsedTime = 0;

        rotIndex = p.rotIndex;

        if (replServer != null)
        {
           
            replServer.MarkUpdate(id);
            //pipeControl.VictoryCheck();
        }

    }

    public Color GetPlayerColor()
    {
        foreach (var colorPair in ConnectionManager.playerTileColorPairs)
        {
            if (mat.GetColor("_Color") == colorPair.Value) return colorPair.Key;
        }
        return Color.red;
    }

    public override void Write(BinaryWriter w)
    { 
        base.Write(w);

        w.Write(xIndex);
        w.Write(yIndex);

        w.Write(tileColor.r);
        w.Write(tileColor.g);
        w.Write(tileColor.b);

        w.Write(rotIndex);
    }
    public override void Read(BinaryReader r)
    { 
        base.Read(r);

        xIndex = r.ReadInt32();
        yIndex = r.ReadInt32();

        tileColor.r = r.ReadSingle();
        tileColor.b = r.ReadSingle();
        tileColor.g = r.ReadSingle();
        tileColor.a = 1.0f;

        int newRotIndex = r.ReadInt32();
        if(newRotIndex != rotIndex)
        {
            Pipe targetTile = pipeControl.pipeMap[xIndex, yIndex];
            StartRotation(targetTile); //rotates on the client
        }
        
    }

}
