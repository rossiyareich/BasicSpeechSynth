using System.Text.RegularExpressions;
using NAudio.Wave;

var ARPABETTable = new Dictionary<string, string[]>();
var ARPABETLines = File.ReadLines(@"Resources/cmudict.0.7a.txt");

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

Console.WriteLine("Hello! type exit; anytime to quit!");

while (true)
{
    Console.Write("Enter text: ");
    var input = Console.ReadLine();

    if (input is null)
    {
        Console.WriteLine("Input can't be empty");
        continue;
    }
    if (input == "exit;")
        break;

    //Get texts and pauses
    bool isNeedTextFix = false;
    var wordsList = input.ToUpper().Split().ToList();
    for (var i = 0; i < wordsList.Count; i++)    //Replace all special characters with a pause
    {
        if (wordsList[i].Contains('.') ||
            wordsList[i].Contains('?') ||
            wordsList[i].Contains('!') ||
            wordsList[i].Contains(',') ||
            wordsList[i].Contains(':') ||
            wordsList[i].Contains(';') ||
            wordsList[i].Contains(@"--") ||
            wordsList[i].Contains('{') ||
            wordsList[i].Contains('}') ||
            wordsList[i].Contains('[') ||
            wordsList[i].Contains(']') ||
            wordsList[i].Contains('(') ||
            wordsList[i].Contains(')') ||
            wordsList[i].Contains('\"') ||
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
    var phonemeList = new List<string>();
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
        Console.WriteLine("Done! Moving on to next text.");
        if (waveFileWriter != null)
        {
            waveFileWriter.Dispose();
        }
    }

}


