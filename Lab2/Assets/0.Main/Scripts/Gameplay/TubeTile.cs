using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using System.Linq;

public class TubeTile : MonoBehaviour
{
    public System.Action Rotated;
    bool rotating = false;
    public float turnDuration = 0.5f;
    float elapsedTime = 0;
    int rotIndex = 0;

    Dictionary<Color, Color> playerTileColorPairs = new Dictionary<Color, Color>
    {
        {Color.red, new Color(1f, 0.6f, 0.6f, 1f) },
        {Color.blue, new Color(0.4f, 1f, 1f, 1f)},
        {Color.green, new Color(0.6f, 1f, 0.5f, 1f) },
        {Color.yellow, new Color(1f, 0.9f, 0.4f, 1f) },
        {Color.magenta, new Color(1f, 0.5f, 0.9f, 1.0f)},
        {Color.cyan, new Color(0.6f, 0.9f, 0.8f, 1f) }
    };

    Color tileColor;

    //baseHDRcombo colorPair;
    Material mat;


    Vector3[] rotations =
    {
        new Vector3 (0,0,90),
        new Vector3 (0,0,180),
        new Vector3 (0,0,270),
        new Vector3 (0,0,360)
    };

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }
    void Start()
    {

        mat = GetComponent<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    public void EnableEmission()
    {
        
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        mat.SetColor("_EmissionColor", tileColor * 1f);
    }

    public void DisableEmission()
    {
        //mat.DisableKeyword("_EMISSION");
        //mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;


        mat.SetColor("_EmissionColor", Color.black);
    }

    public void StartRotation()
    {
        if (!rotating)
        {
            if (rotIndex >= rotations.Length) rotIndex = 0;


            Quaternion targetRot = Quaternion.Euler(rotations[rotIndex]);
            StartCoroutine(Rotate(targetRot));
        }
        
    }

    IEnumerator Rotate(Quaternion targetRot)
    {
        
        while (elapsedTime <= turnDuration)
        {
            elapsedTime += Time.deltaTime;
            rotating = true;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Mathf.Min(elapsedTime, 1.0f));
            Rotated?.Invoke();
            yield return null;
        }
        rotating = false;
        rotIndex++;
        elapsedTime = 0;
    }

    public Color GetPlayerColor()
    {
        foreach(var colorPair in playerTileColorPairs)
        {
            if(mat.GetColor("_Color") == colorPair.Value) return colorPair.Key;
        }
        return Color.red;
    }


}
