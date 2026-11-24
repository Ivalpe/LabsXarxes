using System.Collections;
using System.IO;
using UnityEngine;

public class Microchip : NetObject
{

    [HideInInspector] public Vector3 spawnDir = Vector3.zero;
    [HideInInspector] public float throwStr = 0;
    Renderer renderer;
    ConnectionManager connectionManager = ConnectionManager.Instance;
    int maxFlickers = 10;
    float blinkInterval = 0.5f;
    public bool grabbed = false;
    void Start()
    {
        renderer = GetComponentInChildren<Renderer>();
        StartCoroutine(Flicker());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Write(BinaryWriter w)
    {
        base.Write(w);
    }

    public override void Read(BinaryReader r)
    {
        base.Read(r);

    }

    private void ThrowMicrochip(Vector3 dir, float str)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.AddForce(dir * str, ForceMode.Impulse);
       
    }

    IEnumerator Flicker()
    {
        if (grabbed)
        {
            renderer.enabled = true;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            StopCoroutine(Flicker());
            yield return null;
        }
 
        yield return new WaitForSeconds(5f);

        for(int s = 1; s <= maxFlickers; s++)
        {
            //renderer.enabled = false;
            renderer.gameObject.SetActive(false);
            yield return new WaitForSeconds(blinkInterval);
            renderer.gameObject.SetActive(true);
            yield return new WaitForSeconds(blinkInterval);
            blinkInterval -= (s / 10.0f);
        }
 
        
        connectionManager.replicationServer.MarkDestroy(id);
        connectionManager.linkingContext.Unregister(id);
        Destroy(gameObject);

        //ControlsDisplay ctrlDisplay = FindAnyObjectByType<ControlsDisplay>();
        //ctrlDisplay.HideHoldUI();

        
        
    }



}
