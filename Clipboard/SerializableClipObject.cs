using System;
using System.Collections.Generic;

namespace Clipboard
{
    [Serializable]
    public class SerializableClipObject
    {
        public string Key { get; set; }
        public Dictionary<string, byte[]> ExtractedItems { get; set; }
        public Dictionary<string, string> ExtractedTypeLookupTable { get; set; }
        public Dictionary<string, byte> CompresionTypeLookup { get; set; }
    }
}
