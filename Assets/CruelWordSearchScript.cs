using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class CruelWordSearchScript : MonoBehaviour {

    public KMAudio audio;
    public KMSelectable[] buttons;
    public Color[] colors;

    private static readonly string[] words = {"PERCENT","PRESENT","ELECTRON","ASSEMBLY","ASSAULT","SALVATION","KNOWLEDGE","EXPLOSION","EMERGENCY","DIGITAL","APPLIED","ENTHUSIASM","TENDENCY","MULTIPLY","LANGUAGE","RESTAURANT","COMPUTER","DESCENT","STIMULATE","QUOTATION","CONFUSION","EXAMINE","BACKGROUND","IMPOUND","FASHION","RESTROOM","FRIENDLY","NEUTRAL","CUNNING","PARTNER","BREAKFAST","PRESENCE","PRIVATE","EVALUATE","PROBLEM","UNPLEASANT","DIFFICULTY","TENSION","PROTECTION","ESTABLISH","DRAMATIC","CONVICT","EXPLAIN","CONTAIN","INSTINCT","CHARACTER","MONOPOLY","SELECTION","SLIPPERY","PASTURE","CRIMINAL","ACCIDENT","CHANNEL","DIAMOND","MAJORITY","BUTTOCKS","DIRECTORY","CHEMISTRY","PROMOTE","REPLACE","CHILDISH","PROFOUND","SENTIMENT","OFFSPRING","CREATION","PRESTIGE","ABILITY","PSYCHOLOGY","BATTERY","TEXTURE","BROADCAST","IMPRESS","SERVICE","ABSTRACT","CEREMONY","POSSESSION","MISERABLE","RECYCLE","SHALLOW","CHALLENGE","CONTRARY","PURSUIT","REHEARSAL","ADVENTURE","SOFTWARE","WARRANT","NEIGHBOUR","DIALOGUE","QUARTER","CONSTRAINT","APPRECIATE","MINIMUM","PAVEMENT","TROUBLE","CLASSIFY","PREOCCUPY","LABORATORY","INTENTION","INHIBITION","DISGRACE","VANQUISH","WILDERNESS"};
    private char[] grid = new char[36];
    private int word = -1;
    private int timer = 0;
    private int held = -1;
    private bool unpressable = true;
    private bool activated = false;
    private bool realSolve = false;
    private bool strikeIncoming = false;
    private List<int> correctCells = new List<int>();
    private List<int> selectedCells = new List<int>();
    private Coroutine timerCo;
    private Coroutine[] letterAnims;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
            pressed.OnInteractEnded += delegate () { ReleaseButton(pressed); };
        }
        for (int i = 0; i < grid.Length; i++)
        {
            buttons[i].gameObject.GetComponent<TextMesh>().text = "";
            buttons[i].gameObject.GetComponent<TextMesh>().color = colors[2];
        }
        GetComponent<KMBombModule>().OnActivate += Activate;
    }

    void Start()
    {
        word = UnityEngine.Random.Range(0, words.Length);
        Debug.LogFormat("[Cruel Word Search #{0}] Word: {1}", moduleId, words[word]);
        redo:
        correctCells.Clear();
        grid = new char[36];
        int lastPos = -1;
        for (int i = 0; i < words[word].Length; i++)
        {
            List<int> valids = ValidPositions(i == 0 ? UnityEngine.Random.Range(0, 36) : lastPos);
            lastPos = valids.PickRandom();
            correctCells.Add(lastPos);
            grid[lastPos] = words[word][i];
        }
        for (int i = 0; i < grid.Length; i++)
        {
            if (grid[i] == '\0')
                grid[i] = Convert.ToChar(UnityEngine.Random.Range(65, 91));
        }
        correctCells.Sort();
        List<int> queue;
        List<int> queueIndexes;
        bool firstHit = false;
        for (int i = 0; i < words.Length; i++)
        {
            queue = new List<int>();
            queueIndexes = new List<int>();
            for (int j = 0; j < grid.Length; j++)
            {
                if (grid[j] == words[i][0])
                {
                    queue.Add(j);
                    queueIndexes.Add(0);
                }
            }
            while (queue.Count > 0)
            {
                int curItem = queue[0];
                int curIndex = queueIndexes[0];
                queue.RemoveAt(0);
                queueIndexes.RemoveAt(0);
                if (curIndex == words[i].Length - 1 && word != i)
                    goto redo;
                else if (curIndex == words[i].Length - 1 && word == i)
                {
                    if (!firstHit)
                        firstHit = true;
                    else
                        goto redo;
                }
                else
                {
                    List<int> positions = CheckPositions(curItem, words[i][curIndex + 1]);
                    for (int j = 0; j < positions.Count; j++)
                    {
                        queue.Insert(0, positions[j]);
                        queueIndexes.Insert(0, curIndex + 1);
                    }
                }
            }
        }
        Debug.LogFormat("[Cruel Word Search #{0}] Grid:", moduleId);
        for (int i = 0; i < grid.Length; i+=6)
            Debug.LogFormat("[Cruel Word Search #{0}] {1}{2}{3}{4}{5}{6}", moduleId, grid[i], grid[i + 1], grid[i + 2], grid[i + 3], grid[i + 4], grid[i + 5]);
        if (activated)
            Activate();
    }

    void Activate()
    {
        letterAnims = new Coroutine[36];
        for (int i = 0; i < grid.Length; i++)
            letterAnims[i] = StartCoroutine(LetterAnim(i, true));
        if (!activated)
            activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && unpressable != true)
        {
            pressed.AddInteractionPunch();
            int index = Array.IndexOf(buttons, pressed);
            if (selectedCells.Contains(index))
            {
                held = index;
                timerCo = StartCoroutine(Timer());
            }
            else
            {
                audio.PlaySoundAtTransform("On1", transform);
                selectedCells.Add(index);
                buttons[index].gameObject.GetComponent<TextMesh>().color = colors[1];
            }
        }
    }

    void ReleaseButton(KMSelectable released)
    {
        if (moduleSolved != true && unpressable != true)
        {
            int index = Array.IndexOf(buttons, released);
            if (selectedCells.Contains(index) && timerCo != null)
            {
                string selected = "";
                for (int i = 0; i < selectedCells.Count; i++)
                    selected += buttons[selectedCells[i]].gameObject.GetComponent<TextMesh>().text;
                Debug.LogFormat("[Cruel Word Search #{0}] Selected Letters: {1}", moduleId, selected);
                StopCoroutine(timerCo);
                timerCo = null;
                timer = 0;
                held = -1;
                selectedCells.Sort();
                if (selectedCells.SequenceEqual(correctCells))
                {
                    Debug.LogFormat("[Cruel Word Search #{0}] Correct, module solved!", moduleId);
                    audio.PlaySoundAtTransform("On2", transform);
                    moduleSolved = true;
                    Invoke("Pass", .5f);
                }
                else
                {
                    Debug.LogFormat("[Cruel Word Search #{0}] Incorrect, strike! Resetting...", moduleId);
                    audio.PlaySoundAtTransform("Off2", transform);
                    strikeIncoming = true;
                    Invoke("Strike", .5f);
                    selectedCells.Clear();
                    unpressable = true;
                    letterAnims = new Coroutine[36];
                    for (int i = 0; i < grid.Length; i++)
                        letterAnims[i] = StartCoroutine(LetterAnim(i, false));
                }
            }
        }
    }

    void Pass()
    {
        realSolve = true;
        GetComponent<KMBombModule>().HandlePass();
    }

    void Strike()
    {
        strikeIncoming = false;
        GetComponent<KMBombModule>().HandleStrike();
    }

    List<int> ValidPositions(int curPos)
    {
        List<int> positions = new List<int>();
        int[] offsets = GetOffsets(curPos);
        for (int i = 0; i < 8; i++)
        {
            if (grid[curPos + offsets[i]] == '\0')
                positions.Add(curPos + offsets[i]);
        }
        return positions;
    }

    List<int> CheckPositions(int curPos, char letter)
    {
        List<int> positions = new List<int>();
        int[] offsets = GetOffsets(curPos);
        for (int i = 0; i < 8; i++)
        {
            if (grid[curPos + offsets[i]] == letter)
                positions.Add(curPos + offsets[i]);
        }
        return positions;
    }

    int[] GetOffsets(int curPos)
    {
        int[] offsets;
        if (curPos >= 1 && curPos <= 4)
            offsets = new int[] { 29, 30, 31, -1, 1, 5, 6, 7 };
        else if (curPos >= 31 && curPos <= 34)
            offsets = new int[] { -7, -6, -5, -1, 1, -31, -30, -29 };
        else if (curPos == 6 || curPos == 12 || curPos == 18 || curPos == 24)
            offsets = new int[] { -1, -6, -5, 5, 1, 11, 6, 7 };
        else if (curPos == 11 || curPos == 17 || curPos == 23 || curPos == 29)
            offsets = new int[] { -7, -6, -11, -1, -5, 5, 6, 1 };
        else if (curPos == 0)
            offsets = new int[] { 35, 30, 31, 5, 1, 11, 6, 7 };
        else if (curPos == 5)
            offsets = new int[] { 29, 30, 25, -1, -5, 5, 6, 1 };
        else if (curPos == 30)
            offsets = new int[] { -1, -6, -5, 5, 1, -25, -30, -29 };
        else if (curPos == 35)
            offsets = new int[] { -7, -6, -11, -1, -5, -31, -30, -35 };
        else
            offsets = new int[] { -7, -6, -5, -1, 1, 5, 6, 7 };
        return offsets;
    }

    IEnumerator Timer()
    {
        while (timer != 2)
        {
            yield return new WaitForSeconds(.5f);
            timer++;
        }
        timer = 0;
        selectedCells.Remove(held);
        buttons[held].gameObject.GetComponent<TextMesh>().color = colors[0];
        audio.PlaySoundAtTransform("Off1", transform);
        held = -1;
        timerCo = null;
    }

    IEnumerator LetterAnim(int index, bool show)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(.5f, 1.5f));
        Color initColor = buttons[index].gameObject.GetComponent<TextMesh>().color;
        buttons[index].gameObject.GetComponent<TextMesh>().text = grid[index].ToString();
        for (int i = 0; i <= 25; i++)
        {
            buttons[index].gameObject.GetComponent<TextMesh>().color = Color.Lerp(initColor, show ? colors[0] : colors[2], Math.Max(0, Math.Min(1, i / (float)25 + UnityEngine.Random.Range(-.25f, .25f))));
            yield return null;
        }
        buttons[index].gameObject.GetComponent<TextMesh>().color = show ? colors[0] : colors[2];
        letterAnims[index] = null;
        if (letterAnims.All(x => x == null))
        {
            if (show)
                unpressable = false;
            else
                Start();
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} select/deselect <coord1> (coord2)... [Selects/Deselects the letter at the specified coordinate(s)] | !{0} submit [Submits the current selection] | Valid coordinates are A1-F6 with letters as column and numbers as row";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (unpressable)
            {
                yield return "sendtochaterror Cannot submit while the module is animating!";
                yield break;
            }
            if (selectedCells.Count == 0)
            {
                yield return "sendtochaterror Cannot submit as no letters are selected!";
                yield break;
            }
            yield return null;
            buttons[selectedCells[0]].OnInteract();
            buttons[selectedCells[0]].OnInteractEnded();
            if (strikeIncoming)
                yield return "strike";
            else
                yield return "solve";
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*select|deselect\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify a coordinate to " + parameters[0].ToLower() + "!";
            else
            {
                char[] letters = { 'A', 'B', 'C', 'D', 'E', 'F' };
                char[] numbers = { '1', '2', '3', '4', '5', '6' };
                List<string> used = new List<string>();
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (!Regex.IsMatch(parameters[i], @"^\s*[A-F][1-6]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        yield return "sendtochaterror!f The specified coordinate '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    if (parameters[0].EqualsIgnoreCase("select") && selectedCells.Contains(6 * Array.IndexOf(numbers, parameters[i][1]) + Array.IndexOf(letters, parameters[i].ToUpper()[0])))
                    {
                        yield return "sendtochaterror The specified coordinate '" + parameters[i] + "' is already selected!";
                        yield break;
                    }
                    if (parameters[0].EqualsIgnoreCase("deselect") && !selectedCells.Contains(6 * Array.IndexOf(numbers, parameters[i][1]) + Array.IndexOf(letters, parameters[i].ToUpper()[0])))
                    {
                        yield return "sendtochaterror The specified coordinate '" + parameters[i] + "' is already deselected!";
                        yield break;
                    }
                    if (used.Contains(parameters[i].ToUpper()))
                    {
                        yield return "sendtochaterror The specified coordinate '" + parameters[i] + "' cannot be " + parameters[0].ToLower() + "ed more than once!";
                        yield break;
                    }
                    used.Add(parameters[i].ToUpper());
                }
                if (unpressable)
                {
                    yield return "sendtochaterror Cannot " + parameters[0].ToLower() + " letters while the module is animating!";
                    yield break;
                }
                yield return null;
                for (int i = 1; i < parameters.Length; i++)
                {
                    int index = 6 * Array.IndexOf(numbers, parameters[i][1]) + Array.IndexOf(letters, parameters[i].ToUpper()[0]);
                    if (parameters[0].EqualsIgnoreCase("select"))
                    {
                        buttons[index].OnInteract();
                        buttons[index].OnInteractEnded();
                        if (i != parameters.Length - 1)
                            yield return new WaitForSeconds(.2f);
                    }
                    else
                    {
                        buttons[index].OnInteract();
                        while (timerCo != null) yield return "trycancel Halted deselecting due to a cancel request.";
                        buttons[index].OnInteractEnded();
                    }
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (strikeIncoming)
        {
            StopAllCoroutines();
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            yield break;
        }
        while (unpressable) yield return true;
        if (!moduleSolved)
        {
            List<int> incorrect = new List<int>();
            for (int i = 0; i < selectedCells.Count; i++)
            {
                if (!correctCells.Contains(selectedCells[i]))
                    incorrect.Add(selectedCells[i]);
            }
            for (int i = 0; i < incorrect.Count; i++)
            {
                buttons[incorrect[i]].OnInteract();
                while (timerCo != null) yield return true;
                buttons[incorrect[i]].OnInteractEnded();
                if (i == incorrect.Count - 1)
                    yield return new WaitForSeconds(.2f);
            }
            for (int i = 0; i < correctCells.Count; i++)
            {
                if (!selectedCells.Contains(correctCells[i]))
                {
                    buttons[correctCells[i]].OnInteract();
                    buttons[correctCells[i]].OnInteractEnded();
                    yield return new WaitForSeconds(.2f);
                }
            }
            buttons[correctCells[0]].OnInteract();
            buttons[correctCells[0]].OnInteractEnded();
        }
        while (!realSolve) yield return true;
    }
}