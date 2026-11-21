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
    float turnDuration = 0.25f;

    float elapsedTime = 0;
    float emissionTimer = 0f;

    public PipeControl pipeControl;

    public int xIndex = 0, yIndex = 0; // client & server fill them in

    public Color tileColor;
    public Material mat;
    public int rotIndex = 0;

    AudioSource pipeSource;

    AudioClip[] pipeRotSFX;
    

    bool initialized = false;

    void Awake()
    {
        //so Read isn't called before the material is initialized, cuz then this code would create a second material instance
        mat = GetComponent<Renderer>().material;
        mat.SetColor("_EmissionColor", Color.black);

        pipeSource = GetComponent<AudioSource>();
    }
    void Start()
    {
        
        pipeControl = FindAnyObjectByType<PipeControl>();
        pipeRotSFX = pipeControl.pipeRotSFX;
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

    public void StartRotation(Pipe p, ReplicationManagerServer replServer = null )
    {
        if (!rotating)
        {

            p.rotIndex = (p.rotIndex + 1) % 4;
            p.exit1++;
            p.exit2++;

           
            if (p.exit1 > 4) p.exit1 -= 4;
            if (p.exit2 > 4) p.exit2 -= 4;

            pipeSource.pitch = Random.Range(0.5f, 2.0f);
            pipeSource.volume = Random.Range(0.5f, 1.0f);
            pipeSource.PlayOneShot(pipeRotSFX[Random.Range(0, pipeRotSFX.Length)]);

            rotIndex = p.rotIndex; 
            Quaternion targetRot = Quaternion.Euler(new Vector3(0,0, rotIndex * 90f));
            StartCoroutine(Rotate(p, targetRot, replServer));
        }
        else
        {
            
            
            // On the client, snap the previous spin so rapid server updates are never dropped
            if (replServer == null)
            {
                StopAllCoroutines(); 
                transform.localRotation = Quaternion.Euler(0f, 0f, rotIndex * 90f);
                rotating = false;
            }
            else
            {
                return; // prevent double-clicking while mid-spin in server
            } 
            
        }

        Debug.Log($"rotating tile: {xIndex}, {yIndex} exits:{p.exit1}, {p.exit2}");

    }

    //Rotation for the tiles in the board
    IEnumerator Rotate(Pipe p, Quaternion targetRot, ReplicationManagerServer replServer = null)
    {
        Quaternion startRot = transform.localRotation;      
        elapsedTime = 0f;                                   
        rotating = true;                                    

        while (elapsedTime < turnDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, Mathf.Clamp01(elapsedTime / turnDuration));
            Debug.Log($"rotating tile: {xIndex}, {yIndex} exits:{p.exit1}, {p.exit2}");
            yield return null;
        }

        transform.localRotation = targetRot; //snap at final rotation
        rotating = false;
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
        //base.Write(w);

        w.Write(xIndex);
        w.Write(yIndex);

        w.Write(tileColor.r);
        w.Write(tileColor.g);
        w.Write(tileColor.b);
        

        w.Write(rotIndex);

        
    }
    public override void Read(BinaryReader r)
    { 
        //base.Read(r);

        xIndex = r.ReadInt32();
        yIndex = r.ReadInt32();

        float cr = r.ReadSingle();
        float cg = r.ReadSingle();
        float cb = r.ReadSingle();
  

        int newRotIndex = r.ReadInt32(); //just for reading

        //creatingTile = r.ReadBoolean();

        if (pipeControl == null) pipeControl = FindAnyObjectByType<PipeControl>();
        if (pipeControl == null || pipeControl.pipeMap == null) return;
        Pipe pipe = pipeControl.pipeMap[xIndex, yIndex];

        if (!initialized) //you initialized board on the server, but never on the client!
        {
            pipe.tileGO = gameObject;
            transform.SetParent(pipeControl.transform);
            transform.localPosition = pipe.boardPos;
            tileColor = new Color(cr,cg,cb,1f);
            mat = GetComponent<Renderer>().material;
            mat.SetColor("_Color", tileColor);
            transform.localRotation = Quaternion.Euler(0, 0, newRotIndex * 90f);

            rotIndex = newRotIndex; //just rotated, so update rotIndex
            pipe.rotIndex = newRotIndex; //update the structure rotIndex as well

            initialized = true;
        }
        else
        {
            if (newRotIndex != rotIndex) 
            {
                pipe.rotIndex = (newRotIndex + 3) % 4;
                //server incremented rotindex already, so subtract 1 (-1 % 4 can return -1, use +3 % 4 instead) to stay at the newrotation in the client
                StartRotation(pipe, null); 
                
            }
        }

        
        
    }

}
