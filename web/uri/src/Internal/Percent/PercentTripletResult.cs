namespace uri;

internal class PercentTripletResult
{
    internal bool Success;
    internal int Value;
    internal int StartOffset;
    internal int NextOffset;
    internal UriDiagnostic? Diagnostic;

    internal PercentTripletResult(
        bool success,
        int value,
        int startOffset,
        int nextOffset,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Value = value;
        StartOffset = startOffset;
        NextOffset = nextOffset;
        Diagnostic = diagnostic;
    }
}
