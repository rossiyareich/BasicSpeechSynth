using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicSpeechSynth
{
    [Flags]
    public enum AudioMode
    {
        File = 1,
        WASAPI = 2
    }
}
