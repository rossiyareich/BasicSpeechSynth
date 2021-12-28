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

InputMode currentInputMode = InputMode.English;
AudioMode currentAudioMode = AudioMode.WASAPI;
int currentVolume = 100;
float currentTempo = 1f;

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
wasapi; => switch output to wasapi (default)
file; => switch output to file
filewasapi; => switch output to both wasapi and file
volume={0-inf}; => sets the volume (default=100)
tempo={0.001=>inf}; => sets the tempo (default=1)
eng; => switch to English (default)
ipa; => switch to IPA (use ARPABET with space => different phoneme, . => pause)
----------------------------");

while (true)
{
    Console.Write("Enter input: ");
    var input = Console.ReadLine();

    if (input.EndsWith(';'))
    {
        if (input.StartsWith("volume="))
        {
            int volume = int.Parse(input[7..^1]);
            if(volume < 0)
            {
                Console.WriteLine("Volume not in range");
                continue;
            }
            currentVolume = volume;
            Console.WriteLine($"Set volume to {currentVolume}");
            continue;
        }
        if (input.StartsWith("tempo="))
        {
            float tempo = float.Parse(input[6..^1]);
            if(tempo < 0.001f)
            {
                Console.WriteLine("Tempo not in range");
                continue;
            }
            currentTempo = tempo;
            Console.WriteLine($"Set tempo to {currentTempo}");
            continue;
        }
    }

    switch (input)
    {
        case null:
            Console.WriteLine("Input can't be empty");
            continue;
        case "exit;":
            goto EOF;
        case "eng;":
            currentInputMode = InputMode.English;
            Console.WriteLine("Switched to English.");
            continue;
        case "ipa;":
            currentInputMode = InputMode.IPA;
            Console.WriteLine("Switched to IPA.");
            continue;
        case "wasapi;":
            currentAudioMode = AudioMode.WASAPI;
            Console.WriteLine("Switched output to WASAPI.");
            continue;
        case "file;":
            currentAudioMode = AudioMode.File;
            Console.WriteLine("Switched output to File.");
            continue;
        case "filewasapi;":
            currentAudioMode = AudioMode.File | AudioMode.WASAPI;
            Console.WriteLine("Switched output to File annd WASAPI.");
            continue;
        default:
            break;
    }

    var phonemeList = new List<string>();
    var wordsList = input.ToUpper().Split().ToList();

    switch (currentInputMode)
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
    MemoryStream outputStream = default;
    CancellationTokenSource source = new CancellationTokenSource();
    CancellationToken token = source.Token;

    {
        var sourceFiles = phonemeList.Select(p => $@"Resources/VoiceSamples/{p}.wav");

        foreach (string sourceFile in sourceFiles)
        {
            using (var reader = new AudioFileReader(sourceFile))
            {
                if (waveFileWriter is null)
                {
                    //Create new writer the first time
                    outputStream = new MemoryStream();
                    var format = WaveFormat.CreateIeeeFloatWaveFormat((int)((float)reader.WaveFormat.SampleRate*currentTempo), reader.WaveFormat.Channels);
                    waveFileWriter = new WaveFileWriter(outputStream, format);
                }

                reader.Volume = (float)currentVolume / 100f;
                reader.CopyTo(waveFileWriter);
            }
        }

        waveFileWriter?.Flush();

        if (currentAudioMode.HasFlag(AudioMode.File))
        {
            outputStream.Position = 0;
            using (FileStream fileStream = new FileStream("result.wav", FileMode.Create, FileAccess.Write))
            {
                outputStream.CopyTo(fileStream);
            }
        }

        if (currentAudioMode.HasFlag(AudioMode.WASAPI))
        {
            outputStream.Position = 0;
            using (var outputDevice = new WasapiOut())
            using (var reader = new WaveFileReader(outputStream))
            {
                outputDevice.PlaybackStopped += PlaybackStopped;
                outputDevice.Init(reader);
                outputDevice.Play();
                try
                {
                    await Task.Delay(-1, token);
                }
                catch
                {
                }
                finally
                {
                    outputDevice.PlaybackStopped -= PlaybackStopped;
                }
            }
        }
    }

    waveFileWriter?.Dispose();
    outputStream?.Dispose();
    Console.WriteLine($"Done! Moving on to next {(currentInputMode == InputMode.IPA ? "phonemes" : "text")}.");

    void PlaybackStopped(object sender, StoppedEventArgs e)
    {
        source.Cancel();
        source.Dispose();
    }

}
EOF:
{ }
