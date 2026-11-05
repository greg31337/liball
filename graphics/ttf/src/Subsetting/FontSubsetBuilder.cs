namespace ttf;

internal class FontSubsetGlyphRequest
{
    int glyphId;
    int[] unicodeSequence;

    internal FontSubsetGlyphRequest(int glyphId, int[] unicodeSequence)
    {
        this.glyphId = glyphId;
        this.unicodeSequence = CopyIntegers(unicodeSequence);
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int[] CopyUnicodeSequence()
    {
        return CopyIntegers(unicodeSequence);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
