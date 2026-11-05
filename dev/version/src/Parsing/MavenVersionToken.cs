namespace version;

internal class MavenVersionToken
{
    internal string text;
    internal bool numeric;
    internal char separatorBefore;

    internal MavenVersionToken(string textValue, bool numericValue, char separatorBeforeValue)
    {
        System.ArgumentNullException.ThrowIfNull(textValue);

        if (textValue.Length == 0)
        {
            throw new System.ArgumentException("Maven token text cannot be empty.");
        }

        text = textValue;
        numeric = numericValue;
        separatorBefore = separatorBeforeValue;
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

    internal bool HasSeparatorBefore()
    {
        return separatorBefore != VersionChar.EndOfInput;
    }

    internal char GetSeparatorBefore()
    {
        return separatorBefore;
    }
}
