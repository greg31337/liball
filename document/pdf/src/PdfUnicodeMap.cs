using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pdf;

internal class PdfUnicodeMap
{
    internal List<PdfUnicodeMapEntry> _entries = new List<PdfUnicodeMapEntry>();
    internal IReadOnlyList<PdfUnicodeMapEntry> Entries;

    internal PdfUnicodeMap()
    {
        Entries = _entries;
    }

    internal void Add(int characterCode, int unicodeValue)
    {
        ValidateCharacterCode(characterCode);
        ValidateUnicodeValue(unicodeValue);

        _entries.Add(new PdfUnicodeMapEntry(characterCode, unicodeValue));
    }

    internal void AddText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = GetUnicodeValue(text, index);
            int characterCode = unicodeValue;
            if (characterCode > 65535)
            {
                characterCode = 65535;
            }

            AddIfMissing(characterCode, unicodeValue);
            if (char.IsHighSurrogate(text[index]))
            {
                index++;
            }

            index++;
        }
    }

    internal string ToPdfCMap()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("/CIDInit /ProcSet findresource begin");
        builder.AppendLine("12 dict begin");
        builder.AppendLine("begincmap");
        builder.AppendLine("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def");
        builder.AppendLine("/CMapName /Adobe-Identity-UCS def");
        builder.AppendLine("/CMapType 2 def");
        builder.AppendLine("1 begincodespacerange");
        builder.AppendLine("<0000> <FFFF>");
        builder.AppendLine("endcodespacerange");
        int entryIndex = 0;
        while (entryIndex < _entries.Count)
        {
            int blockCount = _entries.Count - entryIndex;
            if (blockCount > 100)
            {
                blockCount = 100;
            }

            builder.AppendLine(blockCount.ToString(CultureInfo.InvariantCulture) + " beginbfchar");
            int blockEnd = entryIndex + blockCount;
            while (entryIndex < blockEnd)
            {
                PdfUnicodeMapEntry entry = _entries[entryIndex];
                builder.AppendLine("<" + ToCharacterCodeHex(entry.CharacterCode) + "> <" + ToUnicodeHex(entry.UnicodeValue) + ">");
                entryIndex++;
            }

            builder.AppendLine("endbfchar");
        }

        if (_entries.Count == 0)
        {
            builder.AppendLine("0 beginbfchar");
            builder.AppendLine("endbfchar");
        }
        builder.AppendLine("endcmap");
        builder.AppendLine("CMapName currentdict /CMap defineresource pop");
        builder.AppendLine("end");
        builder.Append("end");

        return builder.ToString();
    }

    internal static bool NeedsUnicodeEncoding(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        foreach (char character in text)
        {
            if (character < 32 && character != '\t' && character != '\n' && character != '\r')
            {
                return true;
            }

            if (character > 126)
            {
                return true;
            }
        }

        return false;
    }

    internal static string EncodeTextString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (NeedsUnicodeEncoding(text))
        {
            return "<" + EncodeUtf16Hex(text) + ">";
        }

        return "(" + EscapeLiteralString(text) + ")";
    }

    internal static string EncodeUtf16Hex(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(text);
        return "FEFF" + Convert.ToHexString(bytes);
    }

    internal static string EscapeLiteralString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }

    internal void AddIfMissing(int characterCode, int unicodeValue)
    {
        foreach (PdfUnicodeMapEntry entry in _entries)
        {
            if (entry.CharacterCode == characterCode && entry.UnicodeValue == unicodeValue)
            {
                return;
            }
        }

        Add(characterCode, unicodeValue);
    }

    internal static int GetUnicodeValue(string text, int index)
    {
        if (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
        {
            return char.ConvertToUtf32(text[index], text[index + 1]);
        }

        return text[index];
    }

    internal static string ToCharacterCodeHex(int value)
    {
        return value.ToString("X4", CultureInfo.InvariantCulture);
    }

    internal static string ToUnicodeHex(int value)
    {
        string text = char.ConvertFromUtf32(value);
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(text);
        return Convert.ToHexString(bytes);
    }

    internal static void ValidateCharacterCode(int value)
    {
        if (value < 0 || value > 65535)
        {
            throw new ArgumentOutOfRangeException("characterCode", "Character code must fit in two bytes.");
        }
    }

    internal static void ValidateUnicodeValue(int value)
    {
        if (value < 0 || value > 1114111)
        {
            throw new ArgumentOutOfRangeException("unicodeValue", "Unicode value is outside the valid range.");
        }
    }
}
