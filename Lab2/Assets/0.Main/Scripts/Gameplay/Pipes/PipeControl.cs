using System.Collections.Generic;
using System.IO.Pipes;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TubeTile;

public class Pipe
{
    public int exit1, exit2;
    public int type; //1 Recta como el 1 / 0 Curva como el 0

    public Vector3 boardPos = Vector3.zero;
    public GameObject prefab;

    public Color tileColor;

    public GameObject tileObj;


}

public class PipeControl : MonoBehaviour
{

    public GameObject cornerTile, straightTile;
    // PIPES PUZZLE https://hexapipes.vercel.app/custom

    BoxCollider boardDetectArea;
    public List<Color> activePlayerColors = new List<Color>();
    float tileW = 0;
    float tileH = 0;
    

    public Pipe[,] pipeMap;
    public string tablero;

    public static System.Action<Pipe[,]> OnBoardCreated;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        //Initialize the pipes
        pipeMap = new Pipe[10,5];

        

        for (int i = 0; i < pipeMap.GetLength(0); i++)
        {
            for (int j = 0; j < pipeMap.GetLength(1); j++)
            {
                Pipe p = new Pipe();

                int r = Random.Range(0, 2);

                if (r == 0)
                {
                    p.exit1 = 1;
                    p.exit2 = 2;
                    p.prefab = cornerTile;
                }
                else
                {
                    p.exit1 = 1;
                    p.exit2 = 3;
                    p.prefab = straightTile;
                }

                r = Random.Range(0, 4);

                p.exit1 += r;
                p.exit2 += r;

                if (p.exit1 > 4) p.exit1 -= 4;
                if (p.exit2 > 4) p.exit2 -= 4;

                p.type = r;
                pipeMap[i, j] = p;

                tileW = p.prefab.transform.localScale.x;
                tileH = p.prefab.transform.localScale.y;

                p.boardPos.x = (tileW * i) -  (tileW * pipeMap.GetLength(0) / 2.0f) + tileW/2;
                p.boardPos.y = (tileH * j) - (tileH * pipeMap.GetLength(1) / 2.0f) + tileH/2;
                //p.boardPos.z = this.gameObject.transform.position.z;

                p.tileObj = Instantiate(p.prefab, this.gameObject.transform);
                p.tileObj.transform.localPosition = p.boardPos;
                

            }
           
        }


        boardDetectArea = GetComponent<BoxCollider>();
        boardDetectArea.isTrigger = true;
        boardDetectArea.size = new Vector3(tileW * pipeMap.GetLength(0), tileH * pipeMap.GetLength(1), 2.5f);
        boardDetectArea.center = new Vector3(0,0, -1f);


        for (int i = 0; i < pipeMap.GetLength(0); i++)
        {
            for (int j = 0; j < pipeMap.GetLength(1); j++)
            {
                tablero += pipeMap[i, j].type.ToString();
            }
            tablero += "\n";
        }
        Debug.Log(tablero);

        OnBoardCreated?.Invoke(pipeMap);

        ApplyTileColors();
    }

    void ApplyTileColors()
    {
        foreach(var p in pipeMap)
        {
            if(p.tileObj.TryGetComponent<Renderer>(out Renderer renderer)){
               renderer.material.SetColor("_Color", p.tileColor);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
