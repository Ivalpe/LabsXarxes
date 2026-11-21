using System.IO;
using UnityEngine;

public class Microchip : NetObject
{

    [HideInInspector] public Vector3 spawnDir = Vector3.zero;
    [HideInInspector] public float throwStr = 0;
    bool spawned = false;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void Write(BinaryWriter w)
    {
        base.Write(w);

        //if (!spawned)
        //{
        //    w.Write(spawnDir.x);
        //    w.Write(spawnDir.y);
        //    w.Write(spawnDir.z);

        //    w.Write(throwStr);
            
        //}

    }

    public override void Read(BinaryReader r)
    {
        base.Read(r);

        //if (!spawned)
        //{
        //    spawnDir.x = r.ReadSingle();
        //    spawnDir.y = r.ReadSingle();
        //    spawnDir.z = r.ReadSingle();

        //    throwStr = r.ReadSingle();

        //    Vector3 throwDir = new Vector3(spawnDir.x, spawnDir.y, spawnDir.z);
        //    ThrowMicrochip(throwDir, throwStr);
        //    spawned = true;
        //}

    }

    private void ThrowMicrochip(Vector3 dir, float str)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
            rb.AddForce(dir * str, ForceMode.Impulse);
       
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (gameObject.GetComponent<Player>())
        {

        }
    }
}
