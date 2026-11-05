namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeConfusableRecord
{
    public int[] Source;
    public int[] Target;
    public string Type;
    public string Comment;

    public UnicodeConfusableRecord(int[] source, int[] target, string type, string comment)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(comment);

        if (source.Length == 0)
        {
            throw new ArgumentException("Confusable source must contain at least one code point.", nameof(source));
        }

        if (target.Length == 0)
        {
            throw new ArgumentException("Confusable target must contain at least one code point.", nameof(target));
        }

        Source = source;
        Target = target;
        Type = type;
        Comment = comment;
    }
}
