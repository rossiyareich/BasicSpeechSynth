using System.Text.RegularExpressions;
using BasicSpeechSynth;
using NAudio.Wave;

var ARPABETTable = new Dictionary<string, string[]>();
var ARPABETLines = File.ReadLines(@"Resources/cmudict.0.7a.txt");

var validARPABETChar = new string[]
{
    "AA",
    "AE",
    "AH",
    "AO",
    "AW",
    "AX",
    "AXR",
    "AY",
    "EH",
    "ER",
    "EY",
    "IH",
    "IX",
    "IY",
    "OW",
    "OY",
    "UH",
    "UW",
    "UX",
    "B",
    "CH",
    "D",
    "DH",
    "DX",
    "EL",
    "EM",
    "EN",
    "F",
    "G",
    "HH",
    "JH",
    "K",
    "L",
    "M",
    "N",
    "NG",
    "NX",
    "P",
    "Q",
    "R",
    "S",
    "SH",
    "T",
    "TH",
    "V",
    "W",
    "WH",
    "Y",
    "Z",
    "ZH"
};

InputMode currentMode = InputMode.English;

//Generate dictionary
foreach (var line in ARPABETLines)
{
    if (Regex.IsMatch(line, @";;;*"))
    {
        continue;
    }

    var sections = line.Split();
    string key;
    string[] values = sections[2..];

    key = sections[0].ToString();

    if (Regex.IsMatch(sections[0], @"[^A-Z][A-Z]*")) //Is special character
    {
        if (ARPABETTable.ContainsKey(key))
        {
            if (ARPABETTable[key].Length >= values.Length - 2)
            {
                continue;
            }
            else
            {
                ARPABETTable.Remove(key);
            }
        }
    }
    else if (Regex.IsMatch(sections[0], @".*\([0-9]\)*"))
    {
        continue;
    }

    ARPABETTable.Add(key, values.Select(v => char.IsDigit(v.Last()) ? v[..^1] : v).ToArray());
}

Console.WriteLine(@"Hello!
----------------------------
exit; => exit
eng; => switch to English (default)
ipa; => switch to IPA (use ARPABET with space => different phoneme, . => pause)
----------------------------");

while (true)
{
    Console.Write("Enter input: ");
    var input = Console.ReadLine();

    if (input is null)
    {
        Console.WriteLine("Input can't be empty");
        continue;
    }
    if (input == "exit;")
        break;
    if(input == "eng;")
    {
        currentMode = InputMode.English;
        Console.WriteLine("Switched to English.");
        continue;
    }
    if (input == "ipa;")
    {
        currentMode = InputMode.IPA;
        Console.WriteLine("Switched to IPA.");
        continue;
    }

    var phonemeList = new List<string>();
    var wordsList = input.ToUpper().Split().ToList();

    switch (currentMode)
    {
        case InputMode.English:
            {
                //Get texts and pauses
                bool isNeedTextFix = false;
                for (var i = 0; i < wordsList.Count; i++)    //Replace all special characters with a pause
                {
                    if (wordsList[i].Contains('.') ||
                        wordsList[i].Contains('?') ||
                        wordsList[i].Contains('!') ||
                        wordsList[i].Contains(',') ||
                        wordsList[i].Contains(':') ||
                        wordsList[i].Contains(';') ||
                        wordsList[i].Contains(@"--") ||
                        wordsList[i].Contains("...") ||
                        wordsList[i].Contains(',') ||
                        wordsList[i].Contains('\\') ||
                        wordsList[i].Contains('/'))
                    {
                        wordsList[i] = $"{wordsList[i][..^1]}#|#|#SPECIALCHARACTERPAUSE";

                        if (!ARPABETTable.ContainsKey(wordsList[i][..^26]))
                        {
                            Console.WriteLine($"Word does not exist: {wordsList[i][..^26]}");
                            isNeedTextFix = true;
                        }
                    }
                    else if (!ARPABETTable.ContainsKey(wordsList[i]))
                    {
                        Console.WriteLine($"Word does not exist: {wordsList[i]}");
                        isNeedTextFix = true;
                    }
                }
                if (isNeedTextFix)
                {
                    Console.WriteLine("Please fix the unrecognized words");
                    continue;
                }

                //Get all phonemes
                foreach (var word in wordsList)
                {
                    if (word.Contains("#|#|#SPECIALCHARACTERPAUSE"))
                    {
                        phonemeList.AddRange(ARPABETTable[word[..^26]]);
                        phonemeList.Add("Pause");
                        continue;
                    }

                    phonemeList.AddRange(ARPABETTable[word]);
                }
            }
            break;
        case InputMode.IPA:
            {
                //Get texts and pauses
                bool isNeedTextFix = false;
                for (var i = 0; i < wordsList.Count; i++)    //Replace all . with a pause
                {
                    if (wordsList[i] == ".")
                    {
                        wordsList[i] = "Pause";
                        continue;
                    }
                    else if (!validARPABETChar.Contains(wordsList[i]))
                    {
                        Console.WriteLine($"Phoneme does not exist: {wordsList[i]}");
                        isNeedTextFix = true;
                    }
                }
                if (isNeedTextFix)
                {
                    Console.WriteLine("Please fix the unrecognized phonemes");
                    continue;
                }

                phonemeList.AddRange(wordsList);
            }
            break;
    }


    WaveFileWriter waveFileWriter = default;
    try
    {
        var sourceFiles = phonemeList.Select(p => $@"Resources/VoiceSamples/{p}.wav");

        foreach (string sourceFile in sourceFiles)
        {
            using (WaveFileReader reader = new WaveFileReader(sourceFile))
            {
                if (waveFileWriter == null)
                {
                    //Create new writer the first time
                    waveFileWriter = new WaveFileWriter("result.wav", reader.WaveFormat);
                }
                else
                {
                    if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                    {
                        throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                    }
                }

                reader.CopyTo(waveFileWriter);
            }
        }
    }
    finally
    {
        Console.WriteLine($"Done! Moving on to next {(currentMode == InputMode.IPA ? "phonemes" : "text")}.");
        if (waveFileWriter != null)
        {
            waveFileWriter.Dispose();
        }
    }

}


