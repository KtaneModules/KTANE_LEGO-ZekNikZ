using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LEGO;

public class LEGOTest : MonoBehaviour {
	void Start () {
        /*
        Brick b1 = new Brick(3, 1);
        Brick b2 = new Brick(2, 2);
        //Brick b3 = new Brick(3, 1);

        Structure s = new Structure(4, 4, 4);

        s.AddBrick(b1, 1, 1, 0, Direction.NORTH);
        s.AddBrick(b2, 0, 0, 0, Direction.NORTH);
        //s.AddBrick(b3, -1, 3, 0, Direction.NORTH);

        foreach(Brick piece in s.Pieces) {
            int[][] bounds = piece.GetBounds();
            Debug.LogFormat("Bounds: ({0}, {1}) to ({2}, {3})", bounds[0][0], bounds[0][1], bounds[1][0], bounds[1][1]);
        }
        */

        //Random.InitState(12345);
        StructureGenerator sg = new StructureGenerator();
        sg.Generate();
        List<int[]> pages = sg.GetManualPages();
        int[] page = pages[0];

        printArray(page, 8);
        printArray(page.Rotate(1, 8, 8), 8);
        printArray(page.Rotate(2, 8, 8), 8);
        printArray(page.Rotate(3, 8, 8), 8);

        /*
        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < 10; j++) {
                Debug.LogFormat("i={0} j={1}", i, j);
                if (i == 3 && j == 5) {
                    goto label1;
                }
                if (i == 7 && j == 2) {
                    goto label2;
                }
                if (i == 9 && j == 3) {
                    goto label3;
                }
                label1:;
            }
            label2:;
        }
        label3:
        Debug.Log("DONE");
        */


    }

    public static void printArray(int[] data, int rowLength) {
        string result = "";
        for (int i = 0; i < data.Length; i++) {
            if (i % rowLength == 0) result += "\n";
            result += data[i] + ",";
        }
        Debug.Log(result);
    }
}