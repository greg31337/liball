namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeScriptExtensionsRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public string[] Scripts;

    public UnicodeScriptExtensionsRecord(int startCodePoint, int endCodePoint, string[] scripts)
    {
        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Range end must be greater than or equal to range start.");
        }

        ArgumentNullException.ThrowIfNull(scripts);

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        Scripts = scripts;
    }

    public bool Contains(int codePoint)
    {
        if (codePoint < StartCodePoint)
        {
            return false;
        }

        return codePoint <= EndCodePoint;
    }
}
