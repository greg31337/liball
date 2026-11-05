namespace pdf;

internal class PdfUnicodeMapEntry
{
    internal int CharacterCode;
    internal int UnicodeValue;

    internal PdfUnicodeMapEntry(int characterCode, int unicodeValue)
    {
        CharacterCode = characterCode;
        UnicodeValue = unicodeValue;
    }
}
