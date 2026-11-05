namespace sdp.Tests.Unit.Parsing;

internal class SdpFragmentParser : ISdpParser
{
    private readonly SdpParser parser;

    public SdpFragmentParser()
    {
        parser = new SdpParser();
        parser.ValidateDescriptionStructure = false;
    }

    public SdpParseResult ParseText(string input, SdpParseOptions options)
    {
        return parser.ParseText(input, options);
    }

    public SdpParseResult ParseCharacters(ReadOnlySpan<char> input, SdpParseOptions options)
    {
        return parser.ParseCharacters(input, options);
    }

    public SdpParseResult ParseUtf8Bytes(ReadOnlySpan<byte> input, SdpParseOptions options)
    {
        return parser.ParseUtf8Bytes(input, options);
    }
}
