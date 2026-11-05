namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeBoundaryTestRecord
{
    public UnicodeBoundaryTestRecord(int[] codePoints, bool[] breakBefore, string comment)
    {
        ArgumentNullException.ThrowIfNull(codePoints);
        ArgumentNullException.ThrowIfNull(breakBefore);
        ArgumentNullException.ThrowIfNull(comment);

        if (breakBefore.Length != codePoints.Length + 1)
        {
            throw new ArgumentException("Boundary test records must contain one more boundary marker than code point.", nameof(breakBefore));
        }

        CodePoints = codePoints;
        BreakBefore = breakBefore;
        Comment = comment;
    }

    public int[] CodePoints { get; }

    public bool[] BreakBefore { get; }

    public string Comment { get; }
}
