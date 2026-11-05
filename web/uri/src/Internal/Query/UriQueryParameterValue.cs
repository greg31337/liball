namespace uri;

internal class UriQueryParameterValue
{
    internal string Name;
    internal bool HasValue;
    internal string Value;

    internal UriQueryParameterValue(
        string name,
        bool hasValue,
        string value)
    {
        Name = name;
        HasValue = hasValue;
        Value = value;
    }
}
