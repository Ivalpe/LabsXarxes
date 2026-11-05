using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using System.Linq;

public class TubeTile : MonoBehaviour
{ 
    bool rotating = false;
    public float turnDuration = 0.5f;
    float elapsedTime = 0;
    float emissionTimer = 0f;

    public PipeControl pipeControl;

    public int xIndex = 0, yIndex = 0; // client & server fill them in

    public Color tileColor;
    public Material mat;
    void Start()
    {

        mat = GetComponent<Renderer>().material;
        
        mat.SetColor("_EmissionColor", Color.black);

        pipeControl = GetComponentInParent<PipeControl>();

        //tileColor = mat.GetColor("_Color"); --> gets initiated with prefab color
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

    public void StartRotation(Pipe p)
    {
        if (!rotating)
        {
            if (p.rotIndex > 4) p.rotIndex -= 4;
            if (p.exit1 > 4) p.exit1 -= 4;
            if (p.exit2 > 4) p.exit2 -= 4;

            Quaternion targetRot = Quaternion.Euler(new Vector3(0,0, p.rotIndex * 90f));
            StartCoroutine(Rotate(p, targetRot));
        }
        
    }

    //Rotation for the tiles in the board
    IEnumerator Rotate(Pipe p, Quaternion targetRot)
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
        
        pipeControl.VictoryCheck();
    }

    public Color GetPlayerColor()
    {
        foreach (var colorPair in ConnectionManager.playerTileColorPairs)
        {
            if (mat.GetColor("_Color") == colorPair.Value) return colorPair.Key;
        }
        return Color.red;
    }

}
