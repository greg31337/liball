namespace http;

internal class IntegerParseResult
{
    internal bool IsValid;
    internal bool Overflow;
    internal long Value;
    internal int Consumed;
}
