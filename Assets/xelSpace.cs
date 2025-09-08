using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using rnd = UnityEngine.Random;
using KModkit;
using System.Linq;

public class xelSpace : MonoBehaviour {
    public KMSelectable spaceBar;
    public KMModSettings modSettings;
    public MeshRenderer background;
    public TextMesh displayMesh;
    public Material[] spaceMats;
    int spaceIndex;
    string[] allQuotes = new string[] {"Thats one small step for man one giant leap for mankind ",
    "Space is big Really big You just won't believe how vastly hugely mind bogglingly big it is I mean you may think its a long way down the road to the chemist but thats just peanuts to space ",
    "Who are we We find that we live on an insignificant planet of a humdrum star lost in a galaxy tucked away in some forgotten corner of a universe in which there are far more galaxies than people ",
    "These are the voyages of the Starship Enterprise Its five year mission to explore strange new worlds to seek out new life and new civilizations to boldly go where no man has gone before ",
    "Space It seems to go on and on forever Then you get to the end and a monkey starts throwing barrels at you ",
    "Houston we have a problem ",
    "If you want to have a program for moving out into the universe you have to think in centuries not in decades ",
    "The nitrogen in our DNA the calcium in our teeth the iron in our blood the carbon in our apple pies were made in the interiors of collapsing stars We are made of starstuff ",
    "Since in the long run every planetary society will be endangered by impacts from space every surviving civilization is obliged to become spacefaring ",
    "Every one of us is in the cosmic perspective precious If a human disagrees with you let him live In a hundred billion galaxies you will not find another ",
    "If you wish to make an apple pie from scratch you must first invent the universe ",
    "A long time ago in a galaxy far far away ",
    "There are those who believe that life here began out there far across the universe with tribes of humans who may have been the forefathers of the Egyptians or the Toltecs or the Mayans ",
    "It was the dawn of the third age of mankind, ten years after the Earth Minbari war The Babylon Project was a dream given form ",
    "And the word went forth to every outpost of humanity and they came the Aries the Gemons the Virgos the Scorpios the Pisceans and the Sagitarrans "};
    string usedQuote;
    bool inputting, pressedSpace;
    public KMBombInfo bomb;
    public KMBombModule module;
    public KMAudio sound;
    int moduleId;
    static int moduleIdCounter = 1;
    int currentChar = 0;
    bool solved, countCharacters = true;
    static List<int>[] storedExpectedSpacePresses;
    List<int> usedQuoteExpectedSpacePresses;

    IEnumerator SpaceHandler;

    XelSpaceSettings spaceSettings = new XelSpaceSettings();

    static List<int>[] GetSpacesInQuotes(params string[] quotes)
    {
        return quotes.Select(a => Enumerable.Range(0, a.Length).Where(b => a[b] == ' ').ToList()).ToArray();
    }


    void Awake()
    {
        moduleId = moduleIdCounter++;
        try
        {
            var obtainedSettings = new ModConfig<XelSpaceSettings>("XelSpaceSettings");
            spaceSettings = obtainedSettings.Settings;
            obtainedSettings.Settings = spaceSettings;
            countCharacters = spaceSettings.DisplayProgressPartially;
        }
        catch
        {
            Debug.LogWarningFormat("<Space Settings> Settings do not work as intended! Using default settings!");
            countCharacters = false;
        }
        storedExpectedSpacePresses = GetSpacesInQuotes(quotes: allQuotes);
        spaceBar.OnInteract += delegate { PressSpace(); return false; };
    }

    void Start ()
    {
        spaceIndex = rnd.Range(0, 15);
        background.material = spaceMats[spaceIndex];
        usedQuote = allQuotes[spaceIndex];
        usedQuoteExpectedSpacePresses = storedExpectedSpacePresses[spaceIndex];
        Debug.LogFormat("[Space #{0}] The quote, without punctuation is \"{1}\".", moduleId, allQuotes[spaceIndex]);
        Debug.LogFormat("[Space #{0}] Press the space bar to begin. Then press the space bar when the Xth characters are spaces: {1}", moduleId, usedQuoteExpectedSpacePresses.Select(a => a + 1).Join(", "));
	}

	void PressSpace ()
    {
        spaceBar.AddInteractionPunch();
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, spaceBar.transform);
		if (!solved)
        {
            if (!inputting)
            {
                currentChar = 0;
                displayMesh.text = countCharacters ? (currentChar + 1).ToString() : "";
                displayMesh.color = Color.white;
                SpaceHandler = HandleSpaceCoroutine();
                StartCoroutine(SpaceHandler);
                inputting = true;
            }
            else
            {
                pressedSpace = true;
            }
        }
	}

    IEnumerator HandleSpaceCoroutine()
    {
        
        //float storedActivationTime = (int)bomb.GetTime();
        while (currentChar < usedQuote.Length)
        {
            int storedTime = (int)bomb.GetTime();
            while (storedTime == (int)bomb.GetTime())
                yield return null;
            float progress = (float)currentChar / usedQuote.Length;

            if (countCharacters)
            {
                displayMesh.color = new Color(1, 1, 1, 1f - (progress * 4f));
                displayMesh.text = progress >= 0.25f ? "" : (currentChar + 1).ToString();
            }
            if (usedQuoteExpectedSpacePresses.Contains(currentChar) ^ pressedSpace)
            {
                if (!pressedSpace)
                    Debug.LogFormat("[Space #{0}] Character #{1} was a space, but the space bar was not pressed. Strike!", moduleId, currentChar + 1);
                else
                    Debug.LogFormat("[Space #{0}] Character #{1} was not a space, but the space bar was pressed. Strike!", moduleId, currentChar + 1);
                module.HandleStrike();
                StopCoroutine(SpaceHandler);
                inputting = false;
                pressedSpace = false;
                StopAllCoroutines();
                displayMesh.color = Color.red;
                displayMesh.text = (currentChar + 1).ToString();
                yield break;
            }
            

            currentChar++;
            pressedSpace = false;
        }
        displayMesh.color = Color.green;
        displayMesh.text = "DONE";
        module.HandlePass();
        solved = true;
        sound.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
        Debug.LogFormat("[Space #{0}] Module solved.", moduleId);
    }

    void TwitchHandleForcedSolve()
    {
        StartCoroutine(SpaceAutosolveCoroutine());
    }

    IEnumerator SpaceAutosolveCoroutine()
    {
        if (!inputting)
            spaceBar.OnInteract();
        var idxSpaces = Enumerable.Range(0, usedQuote.Length).Where(a => usedQuote[a] == ' ');
        Debug.LogFormat("<Space #{0}> Spaces in the following indices of the quote: {1}", moduleId, idxSpaces.Join());
        while (!solved)
        {
            while (!idxSpaces.Contains(currentChar))
            {
                if (solved) yield break;
                yield return null;
            }
            if (!pressedSpace)
            {
                spaceBar.OnInteract();
                while (idxSpaces.Contains(currentChar))
                    yield return null;
            }
        }
    }
}
