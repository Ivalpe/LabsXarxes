using System.Collections;
//using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;


public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum TeamColor
    {
        RED, BLUE
    }

    public TeamColor tColor;
    bool nearBoard = false;
    TubeTile tileScript;



    Color tileRed = new Color(1, 0.6f, 0.6f, 1);
    Color tileBlue = new Color(0.4f, 1, 1, 1); //normal "Color" is 0 to 1, not 0 to 255

    Material mat;

    void Start()
    {
        //Debug.Log("Player Script Running");
        mat = GetComponentInChildren<Renderer>().material;

        if (tColor == TeamColor.RED)
        {
            
            mat.SetColor("_Color", tileRed);
        }
        else if (tColor == TeamColor.BLUE)
        {
            
            mat.SetColor("_Color", tileBlue);
        }
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
