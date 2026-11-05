namespace asn1;

internal class PatternParseResult
{
    internal PatternNode? Root;
    internal string ErrorMessage = "";
    internal int ErrorOffset;
    internal bool Success;
}
