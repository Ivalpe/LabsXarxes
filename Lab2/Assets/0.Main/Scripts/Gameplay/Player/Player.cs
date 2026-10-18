using System.Collections;
using System.Collections.Generic;

//using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;


public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private readonly List<Color> playerColors = new List<Color>
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    public Color tColor;
    bool nearBoard = false;
    TubeTile tileScript;



    Color tileRed = Color.red;
    Color tileBlue = Color.blue;

    Material mat;

    void Start()
    {
        //find the client network manager game object
        ClientNetworkManager.Instance.RegisterPlayerScript(this);

    }

    public void SetPlayerColor(byte index)
    {
        tColor = playerColors[index];
    }

    // Update is called once per frame
    void Update()
    {
        
        if (nearBoard)
        {
            Vector2 screenPos = Pointer.current.position.value;
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                
                if (hit.collider.CompareTag("TubeTile"))
                {
                    tileScript = hit.collider.gameObject.GetComponent<TubeTile>();

                    if(mat.GetColor("_Color") == tileScript.GetTileColor())
                    {
                        if (Pointer.current.press.wasPressedThisFrame)
                        {

                            if (tileScript) tileScript.StartRotation();
                        }
                        //Debug.Log("Tapped" + hit.collider.gameObject.name);
                        if (tileScript) tileScript.EnableEmission();


                        //StartCoroutine(RotateTile(hit.collider.gameObject, targetRot));
                        //transform.Rotate(90,0,0);
                    }

                }
                else
                {
                    if(tileScript) tileScript.DisableEmission();
                }

            }

        }
        else
        {
            if (tileScript) tileScript.DisableEmission();
        }   
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Board"))
        {
            nearBoard = true;
            Debug.Log("Near board");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Board"))
        {
            nearBoard = false;
            
        }
    }


   
}
