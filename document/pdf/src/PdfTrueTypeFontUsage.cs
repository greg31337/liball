using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pdf;

internal class PdfTrueTypeFontUsage
{
    internal PdfTrueTypeFont Font;
    internal List<int> _glyphIndices;
    internal List<int> _unicodeValues;
    internal IReadOnlyList<int> GlyphIndices;
    internal IReadOnlyList<int> UnicodeValues;

    internal PdfTrueTypeFontUsage(PdfTrueTypeFont font)
    {
        Font = font;
        _glyphIndices = new List<int>();
        _unicodeValues = new List<int>();
        GlyphIndices = _glyphIndices;
        UnicodeValues = _unicodeValues;
    }

    internal string EncodeText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder encoded = new StringBuilder();
        encoded.Append("<");
        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = ReadUnicodeValue(text, index);
            int glyphIndex = Font.GetGlyphIndex(unicodeValue);
            if (glyphIndex < 0)
            {
                throw new ArgumentException("The selected font does not contain every Unicode value in the text.", "text");
            }

            AddMapping(glyphIndex, unicodeValue);
            encoded.Append(glyphIndex.ToString("X4", CultureInfo.InvariantCulture));
            if (unicodeValue > 0xFFFF)
            {
                index++;
            }

            index++;
        }

        encoded.Append(">");
        return encoded.ToString();
    }

    internal void AddMapping(int glyphIndex, int unicodeValue)
    {
        for (int index = 0; index < _glyphIndices.Count; index++)
        {
            if (_glyphIndices[index] == glyphIndex)
            {
                return;
            }
        }

        _glyphIndices.Add(glyphIndex);
        _unicodeValues.Add(unicodeValue);
    }

    internal void Merge(PdfTrueTypeFontUsage usage)
    {
        for (int index = 0; index < usage.GlyphIndices.Count; index++)
        {
            AddMapping(usage.GlyphIndices[index], usage.UnicodeValues[index]);
        }
    }

    internal static int ReadUnicodeValue(string text, int index)
    {
        char character = text[index];
        if (char.IsHighSurrogate(character))
        {
            if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
            {
                throw new ArgumentException("Text contains an unpaired UTF-16 surrogate.", "text");
            }

            return char.ConvertToUtf32(character, text[index + 1]);
        }

        if (char.IsLowSurrogate(character))
        {
            throw new ArgumentException("Text contains an unpaired UTF-16 surrogate.", "text");
        }

        return character;
    }
}
