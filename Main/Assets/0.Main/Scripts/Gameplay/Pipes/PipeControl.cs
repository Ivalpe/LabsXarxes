using System.Collections.Generic;
using UnityEngine;
public class Pipe
{
    //Declare the two types of "exits" a pipe has to check if its being resolved well
    public int exit1, exit2;
    //SETTING WHAT TYPE OF PIPE IT IS
    public int type = 0;//1 Recta como el 1 / 0 Curva como el 0

    public Vector3 boardPos = Vector3.zero;
    public GameObject prefab = null;

    public GameObject tileGO = null;


    public Color tileColor = Color.white;

    public int rotIndex = 0;

    

}

public class PipeControl : MonoBehaviour
{

    //Set up the pipes with the tiles and the colors needed
    public static PipeControl Instance { get; private set; }
    public GameObject cornerTile, straightTile;

    public Texture correctCorner, correctStraight;

    [Range(0, 1)]
    [SerializeField] public float dropChance = 0.1f;
    [SerializeField] public float dropStrength = 2.0f;
    [SerializeField] public GameObject dropPrefab;


    BoxCollider boardDetectArea;
    public List<Color> activePlayerColors = new List<Color>();
    float tileW = 0;
    float tileH = 0;

    bool sentToServer = false;
    ConnectionManager connectionManager;
    public Pipe[,] pipeMap;
    public string tablero;

    public List<Pipe> correctTiles = new List<Pipe>();

    public static System.Action<Pipe[,]> OnBoardCreated;

    ClientNetworkManager networkManager;

    public AudioClip[] pipeRotSFX; //will be retrieved by each tile

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Creates the data to ensure a right path and randomizes the rest of the board
        GenerateBoardData();

        SetUpDetectArea();

        //get network manager
        networkManager = ClientNetworkManager.Instance;



    }

    void GenerateBoardData()
    {
        //Initialize the pipes
        pipeMap = new Pipe[10, 5];

        //numbers 1 and 0 are part of the solution number 2 will be randomized
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
                p.exit1 -= r;
                p.exit2 -= r;

                if (p.exit1 < 1) p.exit1 += 4;
                if (p.exit2 < 1) p.exit2 += 4;

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

    //Debug to check everything generates ok
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

        //sentToServer = GameObject.FindGameObjectWithTag("TubeTile");

        if (connectionManager != null && !sentToServer)
        {
            connectionManager.CreateBoard(pipeMap, this.gameObject);
            sentToServer = true;
        }
    }

    //Check for the right path or good conections to reach the end /STILL IN PROGRESS/
    public void VictoryCheck()
    {
        correctTiles.Clear();
        if (connectionManager == null)
            connectionManager = FindAnyObjectByType<ConnectionManager>();
        if (connectionManager == null) return;

        int w = pipeMap.GetLength(0), h = pipeMap.GetLength(1);
        int xp = 0, yp = h - 1;

        int dir = 2;

        for (int steps = 0; steps < w * h; steps++)
        {
            if (xp < 0 || xp >= w || yp < 0 || yp >= h) return;
            Pipe p = pipeMap[xp, yp];
            if (p == null) return;

            int entry = dir + 2 > 4 ? dir - 2 : dir + 2;
            if (p.exit1 != entry && p.exit2 != entry) return;
            else correctTiles.Add(p);

            dir = (p.exit1 == entry) ? p.exit2 : p.exit1;

            if (xp == w - 1 && yp == 0)
            {
                if (dir == 2)
                {
                    foreach (var t in correctTiles)
                    {
                        Texture correctTex = t.type == 0 ? correctCorner : correctStraight;
                        t.tileGO.GetComponent<Renderer>().material.SetTexture("_BaseMap", correctTex);
                    }

                    connectionManager.DebugWinGame();
                    return;
                }

                switch (dir)
                {
                    case 1: yp += 1; break;
                    case 2: xp += 1; break;
                    case 3: yp -= 1; break;
                    case 4: xp -= 1; break;
                }
            }
        }



    }
}
