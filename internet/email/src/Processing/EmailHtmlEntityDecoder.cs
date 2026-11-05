using System;
using System.Net;
using System.Text;

namespace email;

class EmailHtmlEntityMatch
    {
        string text;
        int consumedCharacters;
        bool decoded;
        bool malformed;

        internal EmailHtmlEntityMatch(string text, int consumedCharacters, bool decoded, bool malformed)
        {
            this.text = text;
            this.consumedCharacters = consumedCharacters;
            this.decoded = decoded;
            this.malformed = malformed;
        }

        internal string GetText() { return text; }
        internal int GetConsumedCharacters() { return consumedCharacters; }
        internal bool WasDecoded() { return decoded; }
        internal bool WasMalformed() { return malformed; }
}
