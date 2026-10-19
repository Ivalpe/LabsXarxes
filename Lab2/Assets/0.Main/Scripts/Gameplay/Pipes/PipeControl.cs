using Unity.VisualScripting;
using UnityEngine;

public class PipeControl : MonoBehaviour
{

    // PIPES PUZZLE https://hexapipes.vercel.app/custom

    public class Pipe
    {
        public int exit1, exit2;
        public int type; //1 Recta como el 1 / 0 Curva como el 0

    }

    public Pipe[,] pipeMap;
    public string tablero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Initialize the pipes
        pipeMap = new Pipe[5, 10];



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
                }
                else
                {
                    p.exit1 = 1;
                    p.exit2 = 3;
                }

                r = Random.Range(0, 4);

                p.exit1 += r;
                p.exit2 += r;

                if (p.exit1 > 4) p.exit1 -= 4;
                if (p.exit2 > 4) p.exit2 -= 4;

                p.type = r;
                pipeMap[i, j] = p;
            }
        }

        
        for (int i = 0; i < pipeMap.GetLength(0); i++)
        {
            for (int j = 0; j < pipeMap.GetLength(1); j++)
            {
                tablero += pipeMap[i, j].type.ToString();
            }
            tablero += "\n";
        }
        Debug.Log(tablero);

    }

    // Update is called once per frame
    void Update()
    {

    }
}
