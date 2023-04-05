using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class colouredCylinder : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMColorblindMode Colorblind;

    public TextMesh[] colorblindTexts; //Main, Top, mid, bottom

    static int ModuleIdCounter = 1;
    int ModuleId;
    bool ModuleSolved;
    bool colorblindActive;

    public static readonly Color[] validColours = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.white, Color.black };
    Color neutralColour = Color.grey;
    string[] colorNamesListFirst = { "R", "G", "B", "Y", "M", "W", "K" };
    string[] colorFullNamesList = { "Red", "Green", "Blue", "Yellow", "Magenta", "White", "Black" };

    int[] buttonValues = new int[3]; //Bottom, Middle, Top

    string unmodifiedBinary;
    string modifiedBinary;

    int[,] smallColourIndexes = new int[3, 2];
    bool isSmallFlashing = false;

    int modifyIndex;

    List<int> colourIndexes = new List<int>();

    public Renderer[] smallMaterials; //Top to bottom
    public Renderer mainMaterial;


    public KMSelectable[] smallButtons;
    public KMSelectable bigButton;

    int targetNumber;
    int inputtedNumber;
    int inputs;

    int[] possibleAnswerVals = new int[6];


    string[,] data = {
        {"1", "0", "1", "1", "0", "1", "1" },
        {"0", "1", "1", "1", "1", "1", "0" },
        {"1", "0", "0", "0", "1", "1", "1" },
        {"1", "1", "1", "1", "0", "0", "1" },
        {"1", "1", "0", "1", "0", "1", "0" },
        {"0", "1", "0", "1", "1", "1", "1" },
        {"1", "1", "1", "1", "0", "1", "1" } };


    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable button in smallButtons) {
            button.OnInteract += delegate () { smallPress(button); return false; };
        }

        bigButton.OnInteract += delegate () { resetPress(); return false; };

        colorblindActive = Colorblind.ColorblindModeActive;
        foreach (TextMesh text in colorblindTexts)
        {
            text.gameObject.SetActive(colorblindActive);
        }
    }

    void Start()
    {
        determineButtonValues();
        for (int i = 0; i < 6; i++)
        {
            int randomIndex = Rnd.Range(0, 3);
            targetNumber += buttonValues[randomIndex];
            possibleAnswerVals[i] = buttonValues[randomIndex];
        }
        Array.Sort(possibleAnswerVals);
        modifiedBinary = convertToBin(targetNumber);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                smallColourIndexes[i, j] = Rnd.Range(0, 7);
            }
        }
        StartCoroutine("FlashSmall");
        isSmallFlashing = true;

        determineModifyIndex();
        unmodifyBinary();


        determineMainColours();
        StartCoroutine("FlashMain");

        string flashesMessage = "";
        for (int i = 0; i < colourIndexes.Count; i++)
        {
            flashesMessage += colorFullNamesList[colourIndexes[i]] + ", ";
        }
        flashesMessage = flashesMessage.Substring(0, flashesMessage.Length - 2);
        Log("The large cylinder is flashing {0}.", flashesMessage);
        Log("The top button is flashing {0} and {1}.", colorFullNamesList[smallColourIndexes[0, 0]], colorFullNamesList[smallColourIndexes[0, 1]]);
        Log("The middle button is flashing {0} and {1}.", colorFullNamesList[smallColourIndexes[1, 0]], colorFullNamesList[smallColourIndexes[1, 1]]);
        Log("The bottom button is flashing {0} and {1}.", colorFullNamesList[smallColourIndexes[2, 0]], colorFullNamesList[smallColourIndexes[2, 1]]);

        Log("The binary gotten from the flashes is {0}.", unmodifiedBinary);
        Log("The binary after modification is {0}. Used the {1} rule.", modifiedBinary, colorFullNamesList[modifyIndex]);
        Log("Your target number was {0}", targetNumber);
        Log("The button values from top to bottom are {0}, {1}, {2}.", buttonValues[2], buttonValues[1], buttonValues[0]);

        string possibleAnswerMessage = "";
        for (int i = 0; i < 6; i++)
        {
            possibleAnswerMessage += possibleAnswerVals[i] + "+";
        }
        possibleAnswerMessage = possibleAnswerMessage.Substring(0, possibleAnswerMessage.Length - 1);
        Log("One possible answer is {0}", possibleAnswerMessage);
    }

    void smallPress(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        inputs++;
        inputtedNumber += buttonValues[2-Array.IndexOf(smallButtons, button)];
        if (isSmallFlashing)
        {
            StopCoroutine("FlashSmall");
            for (int i = 0; i < 3; i++)
            {
                smallMaterials[i].material.color = Color.white;
                colorblindTexts[i + 1].text = "";
            }
            isSmallFlashing = false;
        }
        if (inputs > 6)
        {
            Log("You have made more than 6 inputs! Strike!");
            GetComponent<KMBombModule>().HandleStrike();
            Reset();
            return;
        }
        if (inputs < 6 && inputtedNumber == targetNumber)
        {
            Log("You have got to the target number in less than 6 inputs. Strike!");
            GetComponent<KMBombModule>().HandleStrike();
            Reset();
            return;
        }
        if (inputs == 6 && inputtedNumber != targetNumber)
        {
            Log("You have submitted {0} instead of the target number. Strike!", inputtedNumber);
            GetComponent<KMBombModule>().HandleStrike();
            Reset();
            return;
        }
        if (inputs == 6 && inputtedNumber == targetNumber)
        {
            Log("You have achieved the target number in 6 moves. Solve!");
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, button.transform);
            GetComponent<KMBombModule>().HandlePass();
            StopAllCoroutines();
            ModuleSolved = true;
            foreach (TextMesh text in colorblindTexts)
            {
                text.text = "";
            }
            mainMaterial.material.color = Color.green;
            foreach (Renderer mat in smallMaterials)
            {
                mat.material.color = Color.green;
            }

        }

    }

    void resetPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, bigButton.transform);
        bigButton.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Log("The reset button has been pressed. Resetting inputs!");
        if (inputs > 0)
        {
            Reset();
        }
        
    }

    void Reset()
    {

        inputs = 0;
        inputtedNumber = 0;
        StopAllCoroutines();
        StartCoroutine("FlashSmall");
        StartCoroutine("FlashMain");
        isSmallFlashing = true;
    }

    IEnumerator FlashSmall()
    {
        while (true)
        {
            for (int i = 0; i < 3; i++)
            {
                smallMaterials[i].material.color = validColours[smallColourIndexes[i, 0]];
                if (smallColourIndexes[i, 0] == 5) colorblindTexts[i + 1].color = Color.black;
                else colorblindTexts[i + 1].color = Color.white;
                colorblindTexts[i + 1].text = colorNamesListFirst[smallColourIndexes[i, 0]];
            }
            yield return new WaitForSeconds(0.7f);
            for (int i = 0; i < 3; i++)
            {
                smallMaterials[i].material.color = validColours[smallColourIndexes[i, 1]];
                if (smallColourIndexes[i, 1] == 5) colorblindTexts[i + 1].color = Color.black;
                else colorblindTexts[i + 1].color = Color.white;
                colorblindTexts[i + 1].text = colorNamesListFirst[smallColourIndexes[i, 1]];
            }
            yield return new WaitForSeconds(0.7f);
            for (int i = 0; i < 3; i++)
            {
                smallMaterials[i].material.color = neutralColour;
                colorblindTexts[i + 1].text = "";
            }
            yield return new WaitForSeconds(0.7f);
        }

    }

    IEnumerator FlashMain()
    {
        while (true)
        {
            for (int i = 0; i < colourIndexes.Count; i++)
            {
                mainMaterial.material.color = validColours[colourIndexes[i]];
                if (colourIndexes[i] == 5) colorblindTexts[0].color = Color.black;
                else colorblindTexts[0].color = Color.white;
                colorblindTexts[0].text = colorNamesListFirst[colourIndexes[i]];
                yield return new WaitForSeconds(0.7f);
            }
            mainMaterial.material.color = neutralColour;
            colorblindTexts[0].text = "";
            yield return new WaitForSeconds(0.7f);
        }
    }

    void Update()
    {

    }

    void determineButtonValues()
    {
        buttonValues[0] = Bomb.GetBatteryCount() + Bomb.GetSerialNumberNumbers().Last() + 3;
        buttonValues[1] = buttonValues[0] * 2 + Bomb.GetIndicators().Count();
        buttonValues[2] = buttonValues[1] * 2 + 1;
    }

    void determineModifyIndex()
    {
        string FinalThreeBin = "";
        for (int i = 0; i < 3; i++)
        {
            FinalThreeBin += data[smallColourIndexes[i, 0], smallColourIndexes[i, 1]];
        }
        int index = Helper.BinToDec(FinalThreeBin);
        if (index == 0) index = 1;
        modifyIndex = index - 1;

    }

    void unmodifyBinary()
    {  
        string ub = "";
        switch (modifyIndex)
        {
            case 0:
                ub = modifiedBinary;
                break;
            case 1:
                foreach (char number in modifiedBinary)
                {
                    if (number == '0')
                        ub += "1";
                    else
                        ub += "0";
                }
                break;
            case 2:
                ub = modifiedBinary.Last().ToString() + modifiedBinary.Substring(0, modifiedBinary.Length-1);
                break;
            case 3:
                ub = modifiedBinary.Substring(1, modifiedBinary.Length - 1) + modifiedBinary.Substring(0, 1);
                break;
            case 4:
                string oddPositions = modifiedBinary.Substring(0, modifiedBinary.Length - (int)Math.Floor(modifiedBinary.Length / 2.0));
                string evenPositions = modifiedBinary.Substring(modifiedBinary.Length - (int)Math.Floor(modifiedBinary.Length / 2.0));
                int oddsAppended = 0;
                int evensAppended = 0;
                for (int i = 0; i < modifiedBinary.Length; i++)
                {
                    if (i % 2 == 0)
                    {
                        ub += oddPositions[oddsAppended];
                        oddsAppended++;
                    }
                    else
                    {
                        ub += evenPositions[evensAppended];
                        evensAppended++;
                    }
                }
                break;
            case 5:
                ub = Helper.ReverseString(modifiedBinary.Substring(0, 3)) + modifiedBinary.Substring(3);
                break;
            case 6:
                ub = Helper.ReverseString(modifiedBinary);
                break;
            default:
                break;
        }
        unmodifiedBinary = ub;
    }

    void determineMainColours()
    {
        int initialRow = Rnd.Range(0, 7);
        int initialCol = Rnd.Range(0, 7);
        while (data[initialRow, initialCol] != unmodifiedBinary[0].ToString())
        {
            initialCol++;
            if (initialCol > 6)
            {
                initialCol = 0;
                initialRow++;
            }
            if (initialRow > 6)
            {
                initialCol = 0;
                initialRow = 0;
            }
        }
        colourIndexes.Add(initialRow);
        colourIndexes.Add(initialCol);
        int newRow = initialCol;
        for (int i = 1; i < unmodifiedBinary.Length; i++)
        {
            int newCol = Rnd.Range(0, 7);
            while (data[newRow, newCol] != unmodifiedBinary[i].ToString())
            {
                newCol++;
                if (newCol > 6)
                {
                    newCol = 0;
                }
            }
            colourIndexes.Add(newCol);
            newRow = newCol;

        }
    }

    string convertToBin(int decimalNumber)
    {
        int remainder;
        string result = string.Empty;
        while (decimalNumber > 0)
        {
            remainder = decimalNumber % 2;
            decimalNumber /= 2;
            result = remainder.ToString() + result;
        }
        return result;
    }

    private void Log(string message, params object[] args)
    {
        Debug.LogFormat("[Coloured Cylinder #{0}] {1}", ModuleId, string.Format(message, args));
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} press 1/2/3 to press the top/middle/bottom circular button. Commands can be chained using spaces. Use !{0} reset to press the center cylinder.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.Trim().ToUpper();
        yield return null;
        string[] Commands = Command.Split(' ');
        switch (Commands[0])
        {
            case "PRESS":
                for (int i = 1; i < Commands.Length; i++)
                {
                    if (!"123".Contains(Commands[i]) || Commands[i].Length != 1)
                    {
                        yield break;
                    }
                }
                for (int i = 1; i < Commands.Length; i++)
                {
                    smallButtons[Int32.Parse(Commands[i])-1].OnInteract();
                }
                
                break;
            case "RESET":
                bigButton.OnInteract();
                break;
            default:
                break;
        }
    }
    

IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        Reset();
        int[] answerIndexs = new int[6];
        for (int i = 0; i < 6; i++)
        {
            answerIndexs[i] = 2-Array.IndexOf(buttonValues, possibleAnswerVals[i]);
        }
        for (int i = 0; i < 6; i++)
        {
            smallButtons[answerIndexs[i]].OnInteract();
        }

        
    }
}
