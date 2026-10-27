using UnityEngine;

public class BoardLogic
{
    public PipeData[,] map = new PipeData[10, 5];

    public class PipeData
    {
        public int type;
        public int rotIndex;
        public int exit1, exit2;
    }

    public void Generate()
    {

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

        for (int i = 0; i < map.GetLength(0); i++)
        {
            for (int j = 0; j < map.GetLength(1); j++)
            {
                PipeData p = new PipeData();
                int r = Random.Range(0, 2);

                if (sol[indexSol] == 0)
                {
                    p.exit1 = 1;
                    p.exit2 = 2;
                    p.type = 0;
                }
                else if (sol[indexSol] == 1)
                {
                    p.exit1 = 2;
                    p.exit2 = 4;
                    p.type = 1;
                }
                else
                {
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
                    p.type = r; //type

                }

                indexSol++;

                r = Random.Range(0, 4); //random rotation

                p.rotIndex = r;

                p.exit1 += r;
                p.exit2 += r;

                if (p.exit1 > 4) p.exit1 -= 4;
                if (p.exit2 > 4) p.exit2 -= 4;

                map[i, j] = p;

            }

        }
    }
    public bool RotateTile(int x, int y)
    {
        PipeData p = map[x, y];

        p.rotIndex++;
        p.exit1++;
        p.exit2++;

        if (p.rotIndex > 3) p.rotIndex -= 4;
        if (p.exit1 > 4) p.exit1 -= 4;
        if (p.exit2 > 4) p.exit2 -= 4;

        return VictoryCheck();
    }
    //Check for the right path or good conections to reach the end /STILL IN PROGRESS/
    public bool VictoryCheck()
    {
        int xp = 4, yp = 0;

        PipeData p;
        int dir = 2;
        bool stop = false;
        int pieza = 0;

        while (!stop)
        {
            try
            {
                p = map[xp, yp];


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

                    if (xp == 0 && yp == 9 && (p.exit1 == 2 || p.exit2 == 2))
                    {
                        return true;
                    }
                }
                else stop = true;
            }
            catch (System.NullReferenceException n)
            {
                stop = true;
                break;
            }
        }
        return false;
    }
}
