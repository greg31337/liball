namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNumericValue
{
    public bool HasValue;
    public string Text;
    public long Numerator;
    public long Denominator;

    public UnicodeNumericValue(bool hasValue, string text, long numerator, long denominator)
    {
        HasValue = hasValue;
        Text = text;
        Numerator = numerator;
        Denominator = denominator;
    }

    public static UnicodeNumericValue None()
    {
        return new UnicodeNumericValue(false, "", 0, 1);
    }

    public static UnicodeNumericValue Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return None();
        }

        int slashIndex = text.IndexOf('/');
        if (slashIndex >= 0)
        {
            long numerator = long.Parse(text.Substring(0, slashIndex), System.Globalization.CultureInfo.InvariantCulture);
            long denominator = long.Parse(text.Substring(slashIndex + 1), System.Globalization.CultureInfo.InvariantCulture);
            return new UnicodeNumericValue(true, text, numerator, denominator);
        }

        return new UnicodeNumericValue(true, text, long.Parse(text, System.Globalization.CultureInfo.InvariantCulture), 1);
    }
}
