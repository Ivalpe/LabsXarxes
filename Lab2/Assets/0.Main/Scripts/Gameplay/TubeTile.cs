using UnityEngine;
using System.Collections;
using NUnit.Framework;

public class TubeTile : MonoBehaviour
{

    bool rotating = false;
    public float turnDuration = 0.5f;
    float elapsedTime = 0;
    int rotIndex = 0;
    enum Type
    {
        CORNER,
        STRAIGHT
    }


    public Color hdrColor;
    Material mat;


    Vector3[] rotations =
    {
        new Vector3 (90,0,0),
        new Vector3 (180,0,0),
        new Vector3 (270,0,0),
        new Vector3 (360,0,0)
    };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        mat.SetColor("_EmissionColor", hdrColor * 0.3f);
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
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Mathf.Min(elapsedTime, 1.0f));
            yield return null;
        }
        rotating = false;
        rotIndex++;
        elapsedTime = 0;
    }

    public Color GetTileColor()
    {
        return mat.GetColor("_Color");
    }

}
