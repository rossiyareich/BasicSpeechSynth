# BasicSpeechSynth
**A basic speech synthesizer... pretty self explanatory.**

To use the synthesizer, place the audio samples of ARPABET and the silence sample on to
> Resources/VoiceSamples

Reference https://en.wikipedia.org/wiki/ARPABET on the 2-letter Vowels and Consonants for file names, with the following exceptions:
- IPA h => `HH.wav`
- IPA ŋ => `NG.wav`

Additionally, add a `Pause.wav` file for non-space pauses.

*All files must end with a `.wav` extension and must be of the format 16-bit signed PCM wave audio*
