namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeBidiBracketRecord
{
    public int CodePoint;
    public int PairedBracketCodePoint;
    public string BracketType;

    public UnicodeBidiBracketRecord(int codePoint, int pairedBracketCodePoint, string bracketType)
    {
        CodePoint = codePoint;
        PairedBracketCodePoint = pairedBracketCodePoint;
        BracketType = bracketType;
    }
}
