namespace xml;

internal class XdmDateTimeParserState
{
    internal string Source;
    internal int Index;
    internal bool Valid;
    internal bool Overflow;

    internal XdmDateTimeParserState(string source)
    {
        Source = source;
        Index = 0;
        Valid = true;
    }
}
