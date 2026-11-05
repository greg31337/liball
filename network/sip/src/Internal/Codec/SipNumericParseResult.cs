namespace sip;

internal class SipNumericParseResult
{
    internal bool Success;
    internal int Value;
    internal int CharactersRead;
    internal int ErrorOffset;

    internal SipNumericParseResult(bool success, int value, int charactersRead, int errorOffset)
    {
        Success = success;
        Value = value;
        CharactersRead = charactersRead;
        ErrorOffset = errorOffset;
    }
}
