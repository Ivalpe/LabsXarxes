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

    public GameObject tileGO = new GameObject();


    public Color tileColor = Color.white;

    public int rotIndex = 0;

   
}

public class PipeControl : MonoBehaviour
{

    //Set up the pipes with the tiles and the colors needed
    public static PipeControl Instance { get; private set; }
    public GameObject cornerTile, straightTile;
    

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
        tileW = cornerTile.transform.localScale.x;
        tileH = cornerTile.transform.localScale.y;

        SetUpDetectArea();

        //get network manager
        networkManager = ClientNetworkManager.Instance;

       

    }

    void SetUpDetectArea()
    {
        boardDetectArea = GetComponent<BoxCollider>();
        boardDetectArea.isTrigger = true;
        boardDetectArea.size = new Vector3(tileW * pipeMap.GetLength(0), tileH * pipeMap.GetLength(1), 2.5f);
        boardDetectArea.center = new Vector3(0, 0, -1f);
    }

    private void Update()
    {
        if (connectionManager == null)
            connectionManager = GameObject.FindAnyObjectByType<ConnectionManager>();

        if (connectionManager != null && !sentToServer)
        {
            BuildPipeMap(connectionManager.board);
            connectionManager.CreateBoard(pipeMap, this.gameObject);
            sentToServer = true;
        }
    }

    void BuildPipeMap(BoardLogic board)
    {
        pipeMap = new Pipe[5, 10];

        for (int i = 0; i < pipeMap.GetLength(0); i++)
        {
            for (int j = 0; j < pipeMap.GetLength(1); j++)
            {
                BoardLogic.PipeData d = board.map[i, j];
                Pipe p = new Pipe();

                p.type = d.type;
                p.rotIndex = d.rotIndex;
                p.exit1 = d.exit1;
                p.exit2 = d.exit2;
                p.prefab = d.type == 0 ? cornerTile : straightTile;

                p.boardPos.x = (tileW * i) - (tileW * pipeMap.GetLength(0) / 2.0f) + tileW / 2;
                p.boardPos.y = (tileH * j) - (tileH * pipeMap.GetLength(1) / 2.0f) + tileH / 2;

                pipeMap[i, j] = p;
            }
        }
    }
}
