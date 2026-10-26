using System.Collections.Generic;
using UnityEngine;
public class Pipe
{
    public int exit1, exit2;
    public int type = 0;//1 Recta como el 1 / 0 Curva como el 0

    public Vector3 boardPos = Vector3.zero;
    public GameObject prefab = null;

    public GameObject tileGO = new GameObject();


    public Color tileColor = Color.white;

    public int rotIndex = 0;


    //public void EnableEmission()
    //{

    //    tileGO.GetComponent<Renderer>().material.SetColor("_EmissionColor", tileColor * 0.5f);

    //    //StartCoroutine(DisableEmission());
    //}

    //public void DisableEmission()
    //{

    //    //mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;


    //    tileGO.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.black);


    //}
}

public class PipeControl : MonoBehaviour
{
    public static PipeControl Instance { get; private set; }
    public GameObject cornerTile, straightTile;
    // PIPES PUZZLE https://hexapipes.vercel.app/custom

    BoxCollider boardDetectArea;
    public List<Color> activePlayerColors = new List<Color>();
    float tileW = 0;
    float tileH = 0;

    bool sentToServer = false;
    ConnectionManager connectionManager;
    public Pipe[,] pipeMap;
    public string tablero;

    public static System.Action<Pipe[,]> OnBoardCreated;

    ClientNetworkManager networkManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        GenerateBoardData();

        SetUpDetectArea();

        //get network manager
        networkManager = ClientNetworkManager.Instance;

        //connectionManager = FindAnyObjectByType<ConnectionManager>();

        //if(connectionManager != null)
        //{
        //    connectionManager.CreateBoard(pipeMap);
        //}
    }

    void GenerateBoardData()
    {
        //Initialize the pipes
        pipeMap = new Pipe[10, 5];

        int[] sol = { 2, 2, 0, 1, 0,
                      2, 0, 0, 2, 2,
                      2, 1, 2, 2 ,2,
                      2, 0, 0, 2, 2,
                      2, 2, 0, 1, 0,
                      2, 2, 2, 2, 1,
                      2, 2, 2, 2, 1,
                      2, 2, 2, 2, 1,
                      2, 2, 0, 0, 1,
                      0, 1, 0, 0, 0};
        int indexSol = 0;

        for (int i = 0; i < pipeMap.GetLength(0); i++)
        {
            for (int j = 0; j < pipeMap.GetLength(1); j++)
            {
                Pipe p = new Pipe();
                int r = Random.Range(0, 2);

                if (sol[indexSol] == 0)
                {
                    p.exit1 = 1;
                    p.exit2 = 2;
                    p.prefab = cornerTile;
                    p.type = 0;
                }
                else if (sol[indexSol] == 1)
                {
                    p.exit1 = 2;
                    p.exit2 = 4;
                    p.prefab = straightTile;
                    p.type = 1;
                }
                else
                {
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
                    p.type = r; //type

                }

                indexSol++;

                r = Random.Range(0, 4); //random rotation

                p.rotIndex = r;

                p.exit1 += r;
                p.exit2 += r;

                if (p.exit1 > 4) p.exit1 -= 4;
                if (p.exit2 > 4) p.exit2 -= 4;

                tileW = p.prefab.transform.localScale.x;
                tileH = p.prefab.transform.localScale.y;

                p.boardPos.x = (tileW * i) - (tileW * pipeMap.GetLength(0) / 2.0f) + tileW / 2;
                p.boardPos.y = (tileH * j) - (tileH * pipeMap.GetLength(1) / 2.0f) + tileH / 2;
                //p.boardPos.z = this.gameObject.transform.position.z;


                pipeMap[i, j] = p;

            }

        }

        PrintBoardData();


    }

    void SetUpDetectArea()
    {
        boardDetectArea = GetComponent<BoxCollider>();
        boardDetectArea.isTrigger = true;
        boardDetectArea.size = new Vector3(tileW * pipeMap.GetLength(0), tileH * pipeMap.GetLength(1), 2.5f);
        boardDetectArea.center = new Vector3(0, 0, -1f);
    }


    void PrintBoardData()
    {
        for (int j = pipeMap.GetLength(1) - 1; j >= 0; --j)
        {
            for (int i = 0; i < pipeMap.GetLength(0); i++)
            {
                tablero += pipeMap[i, j].type.ToString() + " ";
            }
            tablero += "\n";
        }
        Debug.Log(tablero);
    }

    private void Update()
    {
        if (connectionManager == null)
            connectionManager = GameObject.FindAnyObjectByType<ConnectionManager>();

        if (connectionManager != null && !sentToServer)
        {
            connectionManager.CreateBoard(pipeMap, this.gameObject);
            sentToServer = true;
        }
    }

    public bool VictoryCheck()
    {
        int xp = 4, yp = 0;

        Pipe p;
        int dir = 2;
        bool stop = false;
        int pieza = 0;

        while (!stop)
        {
            try
            {
                p = pipeMap[xp, yp];


                dir = dir + 2 > 4 ? dir - 2 : dir + 2;
                if (p.exit1 == dir || p.exit2 == dir)
                {
                    if (p.exit1 == dir)
                        dir = p.exit2;
                    else if (p.exit2 == dir)
                        dir = p.exit1;

                    switch (dir)
                    {
                        case 1:
                            yp += 1;
                            break;
                        case 2:
                            xp += 1;
                            break;
                        case 3:
                            yp -= 1;
                            break;
                        case 4:
                            xp -= 1;
                            break;


                    }
                    Debug.Log("PIEZAS BUENA COLOCADAS: " + pieza++);

                    if (xp == 0 && yp == 9 && (p.exit1 == 2 || p.exit2 == 2))
                    {
                        if (networkManager.isHost) networkManager.UDPconnection.SendPacket(networkManager.serializer.SerializeText("Win"));
                        stop = true;
                    }
                }
            }
            catch (System.Exception e)
            {
                stop = true;
            }
        }
        return false;
    }



}
