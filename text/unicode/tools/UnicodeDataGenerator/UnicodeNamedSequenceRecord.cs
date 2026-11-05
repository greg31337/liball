namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNamedSequenceRecord
{
    public string Name;
    public int[] CodePoints;

    public UnicodeNamedSequenceRecord(string name, int[] codePoints)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(codePoints);

        Name = name;
        CodePoints = codePoints;
    }
}
