using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

//using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;



public class Player : NetObject
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public System.Action<Color> OnColorSet;
    public static System.Action<Pipe> OnTileRotated;

    Camera playerCam;
    bool holdingChip = false;
    public Transform paw;
    public float dropChance = 0.1f;

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
    TubeTile prevTileScript;
    PipeControl boardScript;
    ControlsDisplay ctrlDisplay;

    GameObject grabbedChip = null;
    Material mat;

    public byte colorIndex;
    public string playerName = "Player";

    void Start()
    {
        boardScript = FindAnyObjectByType<PipeControl>();
        ctrlDisplay = FindAnyObjectByType<ControlsDisplay>();
        //find the client network manager game object
        if (ClientNetworkManager.Instance != null) ClientNetworkManager.Instance.RegisterPlayerScript(this);

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

        //if (canGrab && Input.GetKeyDown(KeyCode.E))
        //{
        if (grabbedChip != null)
        {
            grabbedChip.gameObject.transform.position = paw.transform.position;
        }

        //}
        playerCam = GetComponentInChildren<Camera>();
        Vector2 screenPos = Pointer.current.position.value;
        Ray ray = playerCam.ScreenPointToRay(screenPos);
        paw.parent.SetPositionAndRotation(ray.origin + ray.direction.normalized, playerCam.transform.rotation);

        Debug.Log("screenPos: " + screenPos);
        //Debug.Log("paw position: " + paw.parent.transform.position);
        if (nearBoard)
        {
            
           
            Debug.DrawRay(ray.origin, ray.direction, Color.red);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                
                if (hit.collider.CompareTag("TubeTile"))
                {
                    

                    tileScript = hit.collider.gameObject.GetComponent<TubeTile>();
                    

                    if (pColor == tileScript.GetPlayerColor())
                    {
                        if (Pointer.current.press.wasPressedThisFrame)
                        {
                            int x = tileScript.xIndex;
                            int y = tileScript.yIndex;
                            
                            Pipe p = boardScript.pipeMap[x, y];
                            
                            OnTileRotated?.Invoke(p);

                        }

                        //Debug.Log("Tapped" + hit.collider.gameObject.name);
                        if (tileScript)
                        {
                            if (prevTileScript)
                                prevTileScript.DisableEmission();

                            tileScript.EnableEmission();
                            prevTileScript = tileScript;
                        } 
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
        //if (boardScript == null)
        //    boardScript = GameObject.FindAnyObjectByType<PipeControl>();
        

        foreach (var p in boardScript.pipeMap)
        {
            if(p.tileGO != null)
            {
                if (p.tileGO.TryGetComponent<TubeTile>(out TubeTile tileScript))
                    tileScript.DisableEmission();
            }
            
        }
    }

    void LateUpdate()
    {
        if (!playerCam) playerCam = GetComponentInChildren<Camera>();
        if (!playerCam) return;

        Ray ray = playerCam.ScreenPointToRay(Pointer.current.position.value);
        paw.parent.SetPositionAndRotation(ray.origin + ray.direction.normalized, playerCam.transform.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<PipeControl>())
        {
            nearBoard = true;
            Debug.Log("Near board");
        }

        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<Microchip>())
        {
            Microchip chip = other.gameObject.GetComponent<Microchip>();
            if (!ctrlDisplay) ctrlDisplay = FindAnyObjectByType<ControlsDisplay>();
            else ctrlDisplay.ShowHoldUI("Grab");
            if (Input.GetKeyDown(KeyCode.E) && !chip.grabbed)
            {
                //chip.gameObject.transform.SetParent(paw.transform);
                
                //holdingChip = true;
                chip.OnGrabbed();
                ctrlDisplay.HideHoldUI();
                grabbedChip = chip.gameObject;
            }


        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponent<PipeControl>())
        {
            nearBoard = false;
            
        }

        if (other.gameObject.GetComponent<Microchip>())
        {
            
            if (!ctrlDisplay) ctrlDisplay = FindAnyObjectByType<ControlsDisplay>(FindObjectsInactive.Include);
            else 
                ctrlDisplay.HideHoldUI();
            
        }
    }

    // Server writes position, rotation, color and name
    public override void Write(BinaryWriter w)
    {
        base.Write(w);
        w.Write(colorIndex);
        w.Write(playerName);
    }

    // Client reads position, rotation, color and name
    public override void Read(BinaryReader r)
    {
        base.Read(r);
        colorIndex = r.ReadByte();
        playerName = r.ReadString();

        // Apply color using your existing method
        SetPlayerColor(colorIndex);

        // Apply material color and name text on client
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null && colorIndex < playerColors.Count)
            rend.material.SetColor("_Color", playerColors[colorIndex]);

        TMPro.TextMeshProUGUI nameText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (nameText != null) nameText.text = playerName;

        if (ClientNetworkManager.Instance != null)
        {
            // Check if this player is the local client (comparing names)
            bool isMyPlayer = (playerName == ClientNetworkManager.Instance.userName);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true; // Server handles physics

            Transform cam = transform.Find("First Person Camera");
            if (cam != null) cam.gameObject.SetActive(isMyPlayer);

            PlayerInput input = GetComponent<PlayerInput>();
            if (input != null) input.enabled = isMyPlayer;

            this.enabled = isMyPlayer;
        }
    }

}
