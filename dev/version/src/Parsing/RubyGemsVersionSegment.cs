namespace version;

internal class RubyGemsVersionSegment
{
    internal string text;
    internal bool numeric;

    internal RubyGemsVersionSegment(string textValue, bool numericValue)
    {
        System.ArgumentNullException.ThrowIfNull(textValue);

        if (textValue.Length == 0)
        {
            throw new System.ArgumentException("RubyGems segment text cannot be empty.");
        }

        text = textValue;
        numeric = numericValue;
    }

    internal string GetText()
    {
        return text;
    }

    internal bool IsNumeric()
    {
        return numeric;
    }

    internal bool IsText()
    {
        return !numeric;
    }
}
