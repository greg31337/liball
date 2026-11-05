using System;

namespace xml;

internal class XmlTokenBuffer
{
    internal ValueBuffer Buffer;

    internal XmlTokenBuffer(int initialCapacity)
    {
        Buffer = new ValueBuffer(initialCapacity);
    }

    internal int TokenCharacterCount()
    {
        return Buffer.CharacterCount();
    }

    internal void AppendTokenCharacter(char value)
    {
        Buffer.AppendCharacter(value);
    }

    internal void AppendTokenCharacters(ReadOnlySpan<char> value)
    {
        Buffer.AppendCharacters(value);
    }

    internal void AppendTokenScalar(int scalar)
    {
        if (scalar < 0 || scalar > 0x10FFFF || scalar >= 0xD800 && scalar <= 0xDFFF)
        {
            throw new ArgumentOutOfRangeException("scalar");
        }

        if (scalar <= 0xFFFF)
        {
            Buffer.AppendCharacter((char)scalar);
            return;
        }

        int supplementary = scalar - 0x10000;
        Buffer.AppendCharacter((char)(0xD800 + supplementary / 0x400));
        Buffer.AppendCharacter((char)(0xDC00 + supplementary % 0x400));
    }

    internal ReadOnlySpan<char> TokenCharacters()
    {
        return Buffer.WrittenCharacters();
    }

    internal string TokenText()
    {
        return Buffer.GetText();
    }

    internal void ClearToken()
    {
        Buffer.ClearCharacters();
    }
}
