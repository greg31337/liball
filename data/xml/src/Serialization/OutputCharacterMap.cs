using System.Collections.Generic;

namespace xml;

internal class OutputCharacterMap
{
    internal List<int> Scalars;
    internal List<string> Replacements;

    internal OutputCharacterMap(XmlCharacterMapEntry[]? entries)
    {
        Scalars = new List<int>();
        Replacements = new List<string>();
        XmlCharacterMapEntry[] effectiveEntries = entries == null ? [] : entries;
        int index = 0;
        while (index < effectiveEntries.Length)
        {
            XmlCharacterMapEntry? entry = effectiveEntries[index];
            if (entry == null || entry.Replacement == null || entry.CodePoint > 0x10FFFF || entry.CodePoint >= 0xD800 && entry.CodePoint <= 0xDFFF)
            {
                throw CreateOutputCharacterMapError("An output character map contains an invalid entry.", XmlErrorCodes.SEPM0018);
            }

            ValidateOutputCharacterMapReplacement(entry.Replacement);
            int scalar = (int)entry.CodePoint;
            int existing = FindOutputCharacterMap(scalar);
            if (existing >= 0)
            {
                Replacements[existing] = entry.Replacement;
            }
            else
            {
                Scalars.Add(scalar);
                Replacements.Add(entry.Replacement);
            }

            index = index + 1;
        }
    }

    internal int FindOutputCharacterMap(int scalar)
    {
        int index = Scalars.Count - 1;
        while (index >= 0)
        {
            if (Scalars[index] == scalar)
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal void ValidateOutputCharacterMapReplacement(string replacement)
    {
        int position = 0;
        while (position < replacement.Length)
        {
            int first = replacement[position];
            if (first >= 0xD800 && first <= 0xDBFF)
            {
                if (position + 1 >= replacement.Length || replacement[position + 1] < 0xDC00 || replacement[position + 1] > 0xDFFF)
                {
                    throw CreateOutputCharacterMapError("An output character map replacement contains an unpaired high surrogate.", XmlErrorCodes.SERE0008);
                }

                position = position + 2;
                continue;
            }

            if (first >= 0xDC00 && first <= 0xDFFF)
            {
                throw CreateOutputCharacterMapError("An output character map replacement contains an unpaired low surrogate.", XmlErrorCodes.SERE0008);
            }

            position = position + 1;
        }
    }

    internal XmlSerializationException CreateOutputCharacterMapError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
