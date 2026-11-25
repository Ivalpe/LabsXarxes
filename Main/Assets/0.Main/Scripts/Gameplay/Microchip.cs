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
    public bool applied = false;
    public bool destroyed = false;
    Coroutine flickerRoutine;
    void Start()
    {
        renderer = GetComponentInChildren<Renderer>();
        flickerRoutine = StartCoroutine(Flicker());
    }

    // Update is called once per frame
    void Update()
    {
        if (grabbed)
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
                renderer.gameObject.SetActive(true);
                Rigidbody rb = GetComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = false;
            }
            
            
        }
    }



    public override void Write(BinaryWriter w)
    {
        base.Write(w);
        w.Write(destroyed);
    }

    public override void Read(BinaryReader r)
    {
        base.Read(r);
        destroyed = r.ReadBoolean();
        if(connectionManager && destroyed)
        {
            connectionManager.replicationServer.MarkDestroy(id);
            connectionManager.linkingContext.Unregister(id);
            Destroy(gameObject);
        }
    }

    private void ThrowMicrochip(Vector3 dir, float str)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.AddForce(dir * str, ForceMode.Impulse);
       
    }

    IEnumerator Flicker()
    {

        yield return new WaitForSeconds(5f);

        for(int s = 1; s <= maxFlickers; s++)
        {
            //renderer.enabled = false;
            if (grabbed) yield break;
            
            renderer.gameObject.SetActive(false);
            yield return new WaitForSeconds(blinkInterval);
            if (grabbed) yield break;

            renderer.gameObject.SetActive(true);
            yield return new WaitForSeconds(blinkInterval);
            blinkInterval -= (s / 10.0f);
        }

        if (grabbed) yield break;
        destroyed = true;

    }

    

}
