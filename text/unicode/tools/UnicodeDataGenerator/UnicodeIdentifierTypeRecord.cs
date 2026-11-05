namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeIdentifierTypeRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public string[] Types;
    public string Comment;

    public UnicodeIdentifierTypeRecord(int startCodePoint, int endCodePoint, string[] types, string comment)
    {
        ArgumentNullException.ThrowIfNull(types);
        ArgumentNullException.ThrowIfNull(comment);

        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Identifier type range end must be greater than or equal to start.", nameof(endCodePoint));
        }

        if (types.Length == 0)
        {
            throw new ArgumentException("Identifier type records must contain at least one type value.", nameof(types));
        }

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        Types = types;
        Comment = comment;
    }
}
