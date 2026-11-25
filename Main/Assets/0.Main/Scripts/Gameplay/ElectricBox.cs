using UnityEngine;
using System.Collections;

public class ElectricBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    bool zapped = false;
    public Transform socket;
    AudioSource _audioSource;

    public AudioClip[] zapSFX;
    Light _light;
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _light = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<Microchip>() && !zapped)
        {
            StartCoroutine(ShortCircuit(other.gameObject));
        }
    }

    private IEnumerator ShortCircuit(GameObject chip)
    {
        zapped = true;

        chip.transform.SetParent(socket.transform);
        chip.transform.position = socket.transform.position;
        yield return new WaitForSeconds(1.0f);
        Destroy(chip);

        if(_audioSource && zapSFX.Length > 0)
        {
            _audioSource.PlayOneShot(zapSFX[Random.Range(0, zapSFX.Length)]);
        }
        

        _light.enabled = false;

        if (ConnectionManager.Instance)
        {
            ConnectionManager.Instance.AddTime(10f);
            
            //byte[] packet = ConnectionManager.Instance.serializer.SerializeTypes(false, MainSerializer.CommandType.AddTime);
            //ConnectionManager.Instance.cudp.UDPSendToAllClients(packet);
        }
        else if (ClientNetworkManager.Instance)
        {

            byte[] packet = ClientNetworkManager.Instance.serializer.SerializeTypes(false, MainSerializer.CommandType.AddTime);
            ClientNetworkManager.Instance.UDPconnection.SendPacket(packet);

        }

        yield return new WaitForSeconds(0.5f);

        _light.enabled = true;
        zapped = false;

        

        
    }

}
