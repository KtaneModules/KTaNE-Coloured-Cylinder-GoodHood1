using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
public static class Helper {

	public static void DoWhatIWantItTo(int[,] arrayYo)
    {
        string[] colours = { "rDEDD", "gren", "bawrfleuuiue", "yeelaw", "meganeata", "whtei", "blakv" };
        for (int i = 0; i < arrayYo.GetLength(0); i++)
        {
            string row = "";
            for (int j = 0; j < arrayYo.GetLength(1); j++)
            {
                row += colours[arrayYo[i, j]];
                row += " ";
            }
            Debug.Log(row);
        }
        
    }

    public static int BinToDec(string binNum)
    {
        int binaryNumber = Int32.Parse(binNum);
        int decimalValue = 0;


        int base1 = 1;

        while (binaryNumber > 0)
        {
            int reminder = binaryNumber % 10;
            binaryNumber = binaryNumber / 10;
            decimalValue += reminder * base1;
            base1 = base1 * 2;
        }

        return decimalValue;
    }

    public static string ReverseString(string s)
    {
        char[] array = s.ToCharArray();
        Array.Reverse(array);
        return new string(array);
    }
}
