namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeBidiMirrorRecord
{
    public int CodePoint;
    public int MirrorCodePoint;

    public UnicodeBidiMirrorRecord(int codePoint, int mirrorCodePoint)
    {
        CodePoint = codePoint;
        MirrorCodePoint = mirrorCodePoint;
    }
}
