namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeIdentifierStatusRecord
{
    public int StartCodePoint;
    public int EndCodePoint;
    public string Status;
    public string Comment;

    public UnicodeIdentifierStatusRecord(int startCodePoint, int endCodePoint, string status, string comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(comment);

        if (endCodePoint < startCodePoint)
        {
            throw new ArgumentException("Identifier status range end must be greater than or equal to start.", nameof(endCodePoint));
        }

        StartCodePoint = startCodePoint;
        EndCodePoint = endCodePoint;
        Status = status;
        Comment = comment;
    }
}
