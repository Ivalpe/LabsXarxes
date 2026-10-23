using System.Collections;
using System.Collections.Generic;
using System.Linq;


//using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;



public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public System.Action<Color> OnColorSet;
    public static System.Action<Pipe> OnTileRotated;
    
    private readonly List<Color> playerColors = new List<Color>
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    
    

    public Color tColor, pColor;
    bool nearBoard = false;
    TubeTile tileScript;
    PipeControl boardScript;

    Material mat;

    void Start()
    {
        boardScript = GameObject.FindAnyObjectByType<PipeControl>();
        //find the client network manager game object
        ClientNetworkManager.Instance.RegisterPlayerScript(this);

    }

    public void SetPlayerColor(byte index)
    {

        pColor = playerColors[index];
        
        if (boardScript) 
            boardScript.activePlayerColors.Add(pColor);

        
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
                    

                    if(pColor == tileScript.GetPlayerColor())
                    {
                        if (Pointer.current.press.wasPressedThisFrame)
                        {
                            int x = tileScript.xIndex;
                            int y = tileScript.yIndex;
                            //if (tileScript) tileScript.StartRotation(boardScript.pipeMap[x, y]);
                            Pipe p = boardScript.pipeMap[x, y];
                            OnTileRotated?.Invoke(p);
                        }
                        //Debug.Log("Tapped" + hit.collider.gameObject.name);
                        if (tileScript) 
                            tileScript.EnableEmission();


                        //StartCoroutine(RotateTile(hit.collider.gameObject, targetRot));
                        //transform.Rotate(90,0,0);
                    }

                }
                else
                {
                    DisableAllTilesEmission();
                }

            }
           

        }
        else
        {
            DisableAllTilesEmission();
        }   
    }

    void DisableAllTilesEmission()
    {
        foreach(var p in boardScript.pipeMap)
        {
            p.tileGO.GetComponent<TubeTile>().DisableEmission();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PipeControl>())
        {
            nearBoard = true;
            Debug.Log("Near board");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<PipeControl>())
        {
            nearBoard = false;
            
        }
    }

}
