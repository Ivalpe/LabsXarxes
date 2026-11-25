using System.Collections;
using System.IO;
using UnityEngine;

public class Microchip : NetObject
{

    [HideInInspector] public Vector3 spawnDir = Vector3.zero;
    [HideInInspector] public float throwStr = 0;

    public bool grabbed = false;
    public bool applied = false;

    [SerializeField] float lifeTime = 5f;
    [SerializeField] int blinks = 10;
    [SerializeField] float blinkInterval = 0.4f;

    ConnectionManager server;      // only exists on the host
    Renderer[] renderers;
    Coroutine lifeRoutine;
    bool visible = true;

    void Start()
    {
        server = ConnectionManager.Instance;
        if (server != null) lifeRoutine = StartCoroutine(Flicker());
        else if (TryGetComponent(out Rigidbody rb)) 
            rb.isKinematic = true;  // clients: position comes from packets
    }

    IEnumerator Flicker()
    {
        yield return new WaitForSeconds(lifeTime);
        for (int i = 0; i < blinks; i++)
        {
            SetVisible(false); 
            yield return new WaitForSeconds(blinkInterval);
            SetVisible(true); 
            yield return new WaitForSeconds(blinkInterval);
            blinkInterval -= i / blinks;
        }
        Destroy(gameObject);
    }

    public void OnGrabbed()
    {
        grabbed = true;
        if (lifeRoutine != null) 
        { 
            StopCoroutine(lifeRoutine); 
            lifeRoutine = null; 
        }
        SetVisible(true);
    }

    void SetVisible(bool value)
    {
        visible = value;
        if (renderers == null) renderers = GetComponentsInChildren<Renderer>(); 
        foreach (var r in renderers) if (r) r.enabled = value;
    }

    void OnDestroy()
    {
        if (server != null && id != 0)
        {
            server.replicationServer.MarkDestroy(id);
            server.linkingContext.Unregister(id);
        }
    }

    public override void Write(BinaryWriter w) 
    { 
        base.Write(w); 
        w.Write(visible); 
    }
    public override void Read(BinaryReader r) 
    { 
        base.Read(r); 
        SetVisible(r.ReadBoolean()); 
    }

    private void ThrowMicrochip(Vector3 dir, float str)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.AddForce(dir * str, ForceMode.Impulse);

    }
}



