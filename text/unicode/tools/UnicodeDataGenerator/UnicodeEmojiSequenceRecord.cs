namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeEmojiSequenceRecord
{
    public int[] StartCodePoints;
    public int[] EndCodePoints;
    public string PropertyName;
    public string Comment;

    public UnicodeEmojiSequenceRecord(int[] startCodePoints, int[] endCodePoints, string propertyName, string comment)
    {
        ArgumentNullException.ThrowIfNull(startCodePoints);
        ArgumentNullException.ThrowIfNull(endCodePoints);

        if (startCodePoints.Length != endCodePoints.Length)
        {
            throw new ArgumentException("Start and end code point arrays must have the same length.");
        }

        StartCodePoints = startCodePoints;
        EndCodePoints = endCodePoints;
        PropertyName = propertyName;
        Comment = comment;
    }

    public bool HasRanges()
    {
        int index = 0;

        while (index < StartCodePoints.Length)
        {
            if (StartCodePoints[index] != EndCodePoints[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
