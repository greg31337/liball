namespace uri;

internal class TemplateOperatorMetadata
{
    internal string Prefix;
    internal string Separator;
    internal bool Named;
    internal string EmptyValue;
    internal bool AllowReserved;

    internal TemplateOperatorMetadata(
        string prefix,
        string separator,
        bool named,
        string emptyValue,
        bool allowReserved)
    {
        Prefix = prefix;
        Separator = separator;
        Named = named;
        EmptyValue = emptyValue;
        AllowReserved = allowReserved;
    }
}
