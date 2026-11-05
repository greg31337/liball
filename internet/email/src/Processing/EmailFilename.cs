using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace email;
internal class EmailFilenameCleaned
{
    string value;
    bool charactersRemoved;
    bool charactersReplaced;

    internal EmailFilenameCleaned(string value, bool charactersRemoved, bool charactersReplaced)
    {
        this.value = value;
        this.charactersRemoved = charactersRemoved;
        this.charactersReplaced = charactersReplaced;
    }

    internal string GetValue()
    {
        return value;
    }

    internal bool WereCharactersRemoved()
    {
        return charactersRemoved;
    }

    internal bool WereCharactersReplaced()
    {
        return charactersReplaced;
    }
}
