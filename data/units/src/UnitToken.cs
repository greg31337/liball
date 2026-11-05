namespace units;

internal class UnitToken
{
    internal UnitTokenKind _kind;
    internal string _text;
    internal int _position;

    internal UnitToken(UnitTokenKind kind, string text, int position)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (position < 0)
        {
            throw new System.ArgumentException("Token position must not be negative.");
        }

        _kind = kind;
        _text = text;
        _position = position;
    }

    internal UnitTokenKind GetKind()
    {
        return _kind;
    }

    internal string GetText()
    {
        return _text;
    }

    internal int GetPosition()
    {
        return _position;
    }
}
